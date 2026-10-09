using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Bag
{
    // SPEED-UPS (Docs/proposals/inventory.md §3.2): Bag items that take time off a running timer.
    // 1. A speed-up is a command at `now`: it moves the timer's end, and nothing reads a clock.
    // 2. It never gives time away: what it finishes completes at `now`, and the rest of a bigger cut is lost.
    // 3. A typed speed-up fits its own kind of timer, a General one fits all; the picker offers the typed first, then
    //    General, smallest first.
    public class Speedups
    {
        private readonly Bag _bag;
        private readonly ICatalog<IItemDefinition> _items;
        private readonly Construction _construction;
        private readonly VillagerTraining _training;

        private readonly CityState _city;
        private readonly Goods.Workshops _workshops;

        public Speedups(Bag bag, ICatalog<IItemDefinition> items, Construction construction, VillagerTraining training, CityState city,
            Goods.Workshops workshops = null)
        {
            _workshops = workshops;
            _city = city;
            _bag = bag;
            _items = items;
            _construction = construction;
            _training = training;
        }

        // Speed-ups were spent: on what, which, and how many.
        public event Action<SpeedJob, string, int> Used;

        public bool Fits(string id, SpeedJob job)
            => _items.TryGet(id, out var item) && item.Kind == ItemKind.Speedup
               && (item.Speeds == SpeedupKind.General || item.Speeds == job.Kind);

        // Seconds left on the timer at `now`; null when it is not running.
        public double? RemainingSeconds(SpeedJob job, double now) => job.Kind switch
        {
            SpeedupKind.Construction => _construction.RemainingSeconds(job.JobId, now),
            SpeedupKind.Workshop => _workshops?.FrontSeconds(job.JobId, now),
            _ => _training.RemainingSeconds(now),
        };

        // The timers running at `now`: the builders' jobs, then the Townhall's line.
        public IEnumerable<SpeedJob> Running(double now)
        {
            foreach (var job in _city.Jobs)
                if (_construction.RemainingSeconds(job.Id, now) > 0) yield return SpeedJob.Construction(job.Id);
            if (_training.RemainingSeconds(now) > 0) yield return SpeedJob.Training();
            if (_workshops == null) yield break;
            foreach (var district in _city.Districts)
                if (_workshops.FrontSeconds(district.Id, now) > 0) yield return SpeedJob.Workshop(district.Id);
        }

        // The first running timer a speed-up fits — where the Bag sends it; null when nothing of its kind runs.
        public SpeedJob? FirstJobFor(string id, double now)
        {
            foreach (var job in Running(now))
                if (Fits(id, job)) return job;
            return null;
        }

        // The speed-ups held that fit: the typed ones, then General, each smallest first.
        public IReadOnlyList<IItemDefinition> For(SpeedJob job)
            => _items.Items.Where(i => Fits(i.Id, job) && _bag.Count(i.Id) > 0)
                .OrderBy(i => i.Speeds == SpeedupKind.General ? 1 : 0).ThenBy(i => i.Seconds).ToList();

        public SpeedupRefusal Refusal(SpeedJob job, string id, int count, double now)
        {
            if (!Fits(id, job)) return SpeedupRefusal.DoesNotFit;
            if (count < 1 || _bag.Count(id) < count) return SpeedupRefusal.NotHeld;
            return RemainingSeconds(job, now) == null ? SpeedupRefusal.NothingRunning : SpeedupRefusal.None;
        }

        // Uses `count` of a speed-up on the timer at `now`. All go, even past what it had left.
        public SpeedupRefusal Use(SpeedJob job, string id, int count, double now)
        {
            var refusal = Refusal(job, id, count, now);
            if (refusal != SpeedupRefusal.None) return refusal;

            var seconds = _items.Get(id).Seconds * count;
            if (job.Kind == SpeedupKind.Construction) _construction.Hurry(job.JobId, seconds, now);
            else if (job.Kind == SpeedupKind.Workshop) _workshops?.Cut(job.JobId, seconds, now);
            else _training.Hurry(seconds, now);
            _bag.Take(id, count);
            Used?.Invoke(job, id, count);
            return SpeedupRefusal.None;
        }

        // AUTO: the fewest speed-ups that finish the timer, wasting less than the smallest one used — or, when all held
        // cannot finish it, everything held. Two candidates, the fewer items winning and the less waste breaking a tie:
        // the largest first topped up by the smallest that covers the rest, and the one smallest item that covers the
        // whole wait alone. Then whatever the plan can do without is dropped, smallest first.
        public IReadOnlyList<(string Id, int Count)> AutoPlan(SpeedJob job, double now)
        {
            var left = RemainingSeconds(job, now);
            if (left == null || left <= 0) return Array.Empty<(string, int)>();

            var held = For(job);
            var byLargest = held.OrderByDescending(i => i.Seconds).ThenBy(i => i.Speeds == SpeedupKind.General ? 1 : 0).ToList();
            var spare = held.ToDictionary(i => i.Id, i => _bag.Count(i.Id));

            var greedy = new List<(string Id, int Count)>();
            var rest = left.Value;
            foreach (var item in byLargest)
            {
                var n = Math.Min(spare[item.Id], (int)Math.Floor(rest / item.Seconds));
                if (n <= 0) continue;
                greedy.Add((item.Id, n));
                spare[item.Id] -= n;
                rest -= n * item.Seconds;
            }

            if (rest > 0)
            {
                var cover = byLargest.AsEnumerable().Reverse().FirstOrDefault(i => spare[i.Id] > 0 && i.Seconds >= rest);
                if (cover != null) Add(greedy, cover.Id, 1);
            }

            var candidates = new List<List<(string Id, int Count)>> { greedy };
            var single = byLargest.AsEnumerable().Reverse().FirstOrDefault(i => i.Seconds >= left.Value);
            if (single != null) candidates.Add(new List<(string, int)> { (single.Id, 1) });

            var finishing = candidates.Where(p => Seconds(p) >= left.Value).ToList();
            if (finishing.Count == 0) return held.Select(i => (i.Id, _bag.Count(i.Id))).ToList();

            var plan = finishing.OrderBy(p => p.Sum(x => x.Count)).ThenBy(Seconds).First().ToList();
            foreach (var id in plan.Select(p => p.Id).OrderBy(id => _items.Get(id).Seconds).ToList())
            {
                var at = plan.FindIndex(p => p.Id == id);
                var size = _items.Get(id).Seconds;
                while (plan[at].Count > 0 && Seconds(plan) - size >= left.Value) plan[at] = (id, plan[at].Count - 1);
            }

            return plan.Where(p => p.Count > 0).ToList();
        }

        private double Seconds(IEnumerable<(string Id, int Count)> plan) => plan.Sum(p => _items.Get(p.Id).Seconds * p.Count);

        private static void Add(List<(string Id, int Count)> plan, string id, int count)
        {
            var at = plan.FindIndex(p => p.Id == id);
            if (at >= 0) plan[at] = (id, plan[at].Count + count);
            else plan.Add((id, count));
        }
    }
}
