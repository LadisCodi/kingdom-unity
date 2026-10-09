using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Research;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.City
{
    // The Townhall trains villagers, one at a time. A press pays the next villager's Food up front — priced by
    // their place in the town, everyone queued counting as arrived — and queues them while there is a bed for
    // them. Each villager's wait grows with their place and is stamped when their clock starts. A villager moving
    // in changes the rent, so every store is counted up to that moment first.
    public class VillagerTraining : ITimedSystem
    {
        public const string FOOD = "Food";

        private readonly CityState _city;
        private readonly ITrainingSettings _settings;
        private readonly ITreasury _treasury;
        private readonly Stores _stores;
        private readonly IBonuses _bonuses;

        public VillagerTraining(CityState city, ITrainingSettings settings, ITreasury treasury, Stores stores, IBonuses bonuses = null)
        {
            _bonuses = bonuses;
            _city = city;
            _settings = settings;
            _treasury = treasury;
            _stores = stores;
        }

        // A villager moved in.
        public event Action Arrived;

        // The next villager's place in the town: the population and everyone already queued.
        public int NextPlace => _city.Population + _city.Trainees.Count;

        public double NextCost => CostAt(NextPlace);

        public TrainRefusal Refusal
        {
            get
            {
                if (NextPlace >= _stores.Housing) return TrainRefusal.NoRoom;
                return _treasury.Get(FOOD) >= NextCost ? TrainRefusal.None : TrainRefusal.CannotAfford;
            }
        }

        public Trainee Current => _city.Trainees.FirstOrDefault();

        public TrainRefusal Train(double now)
        {
            var refusal = Refusal;
            if (refusal != TrainRefusal.None) return refusal;

            _treasury.TryPay(new Dictionary<string, double> { [FOOD] = NextCost });
            _city.Trainees.Add(new Trainee());
            if (_city.Trainees.Count == 1) StartNext(now);
            return TrainRefusal.None;
        }

        // Beds still free for a villager: the houses, less the town and everyone already queued.
        public int Room => Math.Max(0, _stores.Housing - NextPlace);

        // What one press orders at an amount, priced whole: "All" is as many as there are beds and Food for.
        public TrainPlan Plan(TrainAmount amount)
        {
            if (amount != TrainAmount.All)
            {
                var count = amount == TrainAmount.One ? 1 : amount == TrainAmount.Ten ? 10 : 100;
                return new TrainPlan(count, BatchCost(count));
            }

            var food = _treasury.Get(FOOD);
            var all = 1;
            while (all < Room && BatchCost(all + 1) <= food) all++;
            return new TrainPlan(all, BatchCost(all));
        }

        // An order of `count` villagers as one: all of them or none — the beds and the whole price are checked before
        // anything is paid, so a refused order leaves the town as it was.
        public TrainRefusal Train(int count, double now)
        {
            if (count > 1)
            {
                if (Room < count) return TrainRefusal.NoRoom;
                if (_treasury.Get(FOOD) < BatchCost(count)) return TrainRefusal.CannotAfford;
            }

            var first = Train(now);
            if (first != TrainRefusal.None) return first;
            for (var i = 1; i < count; i++) Train(now);
            return TrainRefusal.None;
        }

        // What the next `count` villagers cost together.
        public double BatchCost(int count)
        {
            var total = 0.0;
            for (var i = 0; i < count; i++) total += CostAt(NextPlace + i);
            return total;
        }

        // What the villager at a place costs: the authored list, then each one a growth times the last.
        public double CostAt(int place)
        {
            var first = _settings.CostFirst;
            if (first.Count == 0) return 0;
            if (place < first.Count) return first[place];

            return Prices.RoundPrice(first[first.Count - 1] * Math.Pow(_settings.CostGrowth, place - first.Count + 1));
        }

        // How long the villager at a place takes.
        // The Townhall trains faster with every rank of training speed: the wait is divided by it.
        public double SecondsAt(int place)
            => Math.Round(_settings.Seconds * Math.Pow(_settings.SecondsGrowth, place) / Math.Max(1, _bonuses.Multiplier(TechStats.VILLAGER_TRAINING_SPEED)),
                MidpointRounding.AwayFromZero);

        // Seconds left on the whole line at `now`: the one on the bench, and each behind it at its own place's wait.
        public double? RemainingSeconds(double now)
        {
            if (_city.Trainees.Count == 0) return null;
            var total = 0.0;
            for (var i = 0; i < _city.Trainees.Count; i++)
            {
                var trainee = _city.Trainees[i];
                total += trainee.ArrivesAt is double at ? Math.Max(0, (at - now) / 1000) : SecondsAt(_city.Population + i);
            }

            return total;
        }

        // Takes `seconds` off the line at `now` (a speed-up): whoever it finishes arrives now, the next starts now, and
        // what is left over is lost.
        public void Hurry(double seconds, double now)
        {
            var budget = seconds * 1000;
            while (budget > 0 && Current is { } head)
            {
                if (head.StartedAt == null) StartNext(now);
                var left = Math.Max(0, head.ArrivesAt.Value - now);
                if (budget < left)
                {
                    head.Seconds -= budget / 1000;
                    return;
                }

                head.Seconds = (now - head.StartedAt.Value) / 1000;
                budget -= left;
                ApplyDue(now);
            }
        }

        public double? NextBoundary(double after)
        {
            var at = Current?.ArrivesAt;
            return at > after ? at : null;
        }

        public void ApplyDue(double time)
        {
            while (Current?.ArrivesAt is double at && at <= time)
            {
                _city.Trainees.RemoveAt(0);
                _stores.SettleAll(at);
                _city.Population++;
                _stores.WakeAll(at);
                if (_city.Trainees.Count > 0) StartNext(at);

                Arrived?.Invoke();
            }
        }

        public void RunUntil(double time) { }

        private void StartNext(double now)
        {
            var next = _city.Trainees[0];
            next.StartedAt = now;
            next.Seconds = SecondsAt(_city.Population);
        }
    }
}
