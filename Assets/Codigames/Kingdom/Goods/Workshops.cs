using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Goods.State;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.Goods
{
    // The workshops (Docs/features/17-workshops-and-goods.md §4–§7). A good is paid for whole when it is queued and
    // refunded whole when it is cancelled. The crew is the engine: with nobody in the workshop nothing moves; with
    // `n` villagers the first `k = min(n, items)` items are worked at once, each gaining `n / k` worker-milliseconds
    // a millisecond — counted in whole `k`-millisecond steps, so one replay and stepped ticking agree to the
    // millisecond. A finished good goes straight to the stockpile; the queue's length is the only ceiling.
    public class Workshops : ITimedSystem
    {
        private const double MIN_NEED_MS = 1000;

        private readonly GoodsState _state;
        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly ICatalog<IGoodDefinition> _goods;
        private readonly Stockpile _stockpile;
        private readonly ITreasury _treasury;
        private readonly ManaPool _mana;
        private readonly Workforce _crews;
        private readonly IRushSettings _rush;
        private readonly Adjacency _adjacency;
        private readonly IBonuses _bonuses;

        public Workshops(GoodsState state, CityState city, ICatalog<IBuildingDefinition> buildings, ICatalog<IGoodDefinition> goods,
            Stockpile stockpile, ITreasury treasury, ManaPool mana, Workforce crews, IRushSettings rush, Adjacency adjacency = null,
            IBonuses bonuses = null)
        {
            _state = state;
            _city = city;
            _buildings = buildings;
            _goods = goods;
            _stockpile = stockpile;
            _treasury = treasury;
            _mana = mana;
            _crews = crews;
            _rush = rush;
            _adjacency = adjacency;
            _bonuses = bonuses;

            // A villager arriving or leaving changes the pace: the work until then is counted at the old one.
            crews.CrewChanging += (district, now) => { if (_state.Lines.ContainsKey(district)) Reanchor(district, now); };
        }

        // A good came off a workshop's bench: the district, the good, the moment.
        public event Action<DistrictState, string, double> Made;

        // The queue changed (queued, cancelled, finished): the district.
        public event Action<string> Changed;

        public static bool IsWorkshop(IBuildingDefinition building) => !string.IsNullOrEmpty(building.Production.Produces);

        public IGoodDefinition GoodOf(DistrictState district)
            => _buildings.TryGet(district.DefinitionId, out var b) && IsWorkshop(b) && _goods.TryGet(b.Production.Produces, out var g) ? g : null;

        // How many it may hold queued at once: its level's, and the tree's extra orders.
        public int Capacity(DistrictState district)
        {
            var lengths = _buildings.Get(district.DefinitionId).Production.QueueLengthPerLevel;
            if (lengths.Count == 0) return 0;
            var level = lengths[Math.Min(Math.Max(district.Level, 1), lengths.Count) - 1];
            return (int)Math.Floor(_bonuses?.Apply(TechStats.WORKSHOP_QUEUE_SLOTS, level, TargetKind.District, district.DefinitionId) ?? level);
        }

        public IReadOnlyList<WorkshopItem> Queue(string districtId) => _state.Lines.TryGetValue(districtId, out var line) ? line.Items : Array.Empty<WorkshopItem>();

        // Worker-milliseconds one item of its good takes here, priced now: its neighbours and the tree.
        public double NeedMs(DistrictState district)
        {
            var good = GoodOf(district);
            if (good == null) return 0;
            var neighbours = _adjacency?.Multiplier(district, AdjacencyStat.WorkTime) ?? 1;
            var speed = Math.Max(1, _bonuses?.Multiplier(TechStats.WORKSHOP_SPEED, TargetKind.District, district.DefinitionId) ?? 1);
            return Math.Max(MIN_NEED_MS, Math.Round(good.WorkSeconds * 1000 * neighbours / speed));
        }

        // What stands between the workshop and one more item.
        public WorkshopRefusal Refusal(string districtId, double now)
        {
            var district = District(districtId);
            var good = district == null ? null : GoodOf(district);
            if (good == null) return WorkshopRefusal.NotAWorkshop;
            if (!district.Built) return WorkshopRefusal.NotBuilt;
            if (Queue(districtId).Count >= Capacity(district)) return WorkshopRefusal.QueueFull;
            if (!_treasury.CanAfford(good.Input)) return WorkshopRefusal.NotEnoughResources;
            if (!_stockpile.CanAfford(InputGoods(good))) return WorkshopRefusal.NotEnoughGoods;
            if (good.InputMana > 0 && _mana.Amount < good.InputMana) return WorkshopRefusal.NotEnoughMana;
            return WorkshopRefusal.None;
        }

        // Queues one of its good: paid now, priced now.
        public WorkshopRefusal Enqueue(string districtId, double now)
        {
            var refusal = Refusal(districtId, now);
            if (refusal != WorkshopRefusal.None) return refusal;

            var district = District(districtId);
            var good = GoodOf(district);
            _treasury.TryPay(good.Input);
            _stockpile.TryPay(InputGoods(good));
            if (good.InputMana > 0) _mana.TrySpend(good.InputMana, now);

            Reanchor(districtId, now);
            Line(districtId).Items.Add(new WorkshopItem { Good = good.Id, NeedMs = NeedMs(district) });
            Changed?.Invoke(districtId);
            return WorkshopRefusal.None;
        }

        // Takes an item off the queue and gives back all it cost; the work done on it is lost.
        public WorkshopRefusal Cancel(string districtId, int index, double now)
        {
            if (!_state.Lines.TryGetValue(districtId, out var line)) return WorkshopRefusal.NoSuchItem;
            if (index < 0 || index >= line.Items.Count) return WorkshopRefusal.NoSuchItem;

            Reanchor(districtId, now);
            var item = line.Items[index];
            line.Items.RemoveAt(index);
            if (_goods.TryGet(item.Good, out var good))
            {
                foreach (var (currency, amount) in good.Input) _treasury.Add(currency, amount);
                _stockpile.Refund(InputGoods(good));
                if (good.InputMana > 0) _treasury.Add(ManaPool.MANA, good.InputMana);
            }

            Changed?.Invoke(districtId);
            return WorkshopRefusal.None;
        }

        // An item's work done at `now`, 0 to 1; 0 for one waiting its turn.
        public double Progress(string districtId, int index, double now)
        {
            var items = Queue(districtId);
            if (index < 0 || index >= items.Count || items[index].NeedMs <= 0) return 0;
            return Math.Min(1, WorkAt(districtId, index, now) / items[index].NeedMs);
        }

        // Seconds until the front item is done at the pace now; null when nothing is being worked.
        public double? FrontSeconds(string districtId, double now)
        {
            var items = Queue(districtId);
            var (n, k) = Pace(districtId);
            if (k == 0) return null;
            return Math.Max(0, (items[0].NeedMs - WorkAt(districtId, 0, now)) * k / n / 1000);
        }

        // Seconds until the next item of the queue comes off the bench; null when nothing is being worked.
        public double? NextSeconds(string districtId, double now)
        {
            var items = Queue(districtId);
            var (n, k) = Pace(districtId);
            if (k == 0) return null;
            var soonest = double.MaxValue;
            for (var i = 0; i < k; i++) soonest = Math.Min(soonest, (items[i].NeedMs - WorkAt(districtId, i, now)) * k / n / 1000);
            return Math.Max(0, soonest);
        }

        // The Gems that finish the front item: its time left.
        public double RushCost(string districtId, double now) => GemRush.GemsToFinish(FrontSeconds(districtId, now) ?? 0, _rush.SecondsPerGem);

        public WorkshopRefusal Rush(string districtId, double now)
        {
            if (FrontSeconds(districtId, now) == null) return WorkshopRefusal.NothingWorking;
            var cost = new Dictionary<string, double> { [GemRush.GEMS] = RushCost(districtId, now) };
            if (!_treasury.TryPay(cost)) return WorkshopRefusal.NotEnoughGems;

            Reanchor(districtId, now);
            var front = Line(districtId).Items[0];
            front.WorkMs = front.NeedMs;
            Deliver(districtId, now);
            return WorkshopRefusal.None;
        }

        // A speed-up on the front item: the crew's work for that long, or the item now — what is left over is lost.
        public bool Cut(string districtId, double seconds, double now)
        {
            var (n, k) = Pace(districtId);
            if (k == 0) return false;

            Reanchor(districtId, now);
            var front = Line(districtId).Items[0];
            var work = Math.Floor(seconds * 1000 * n / k);
            if (front.WorkMs + work < front.NeedMs)
            {
                front.WorkMs += work;
                Changed?.Invoke(districtId);
            }
            else
            {
                front.WorkMs = front.NeedMs;
                Deliver(districtId, now);
            }

            return true;
        }

        public double? NextBoundary(double after)
        {
            double? earliest = null;
            foreach (var (districtId, line) in _state.Lines)
            {
                var (n, k) = Pace(districtId);
                for (var i = 0; i < k; i++)
                {
                    var item = line.Items[i];
                    var at = line.Anchor + Math.Ceiling(Math.Max(0, item.NeedMs - item.WorkMs) / n) * k;
                    if (at > after && (earliest == null || at < earliest)) earliest = at;
                }
            }

            return earliest;
        }

        public void ApplyDue(double time)
        {
            foreach (var districtId in _state.Lines.Keys.ToList())
            {
                var (_, k) = Pace(districtId);
                if (k == 0) continue;
                Settle(districtId, time);
                var line = _state.Lines[districtId];
                if (line.Items.Count > 0 && line.Items[0].WorkMs >= line.Items[0].NeedMs) Deliver(districtId, time);
            }
        }

        public void RunUntil(double time) { }

        // Every finished item at the front goes to the stockpile; the pace starts again from `time`.
        private void Deliver(string districtId, double time)
        {
            var line = Line(districtId);
            var district = District(districtId);
            while (line.Items.Count > 0 && line.Items[0].WorkMs >= line.Items[0].NeedMs)
            {
                var good = line.Items[0].Good;
                line.Items.RemoveAt(0);
                _stockpile.Add(good, 1);
                if (district != null) Made?.Invoke(district, good, time);
            }

            line.Anchor = time;
            Changed?.Invoke(districtId);
        }

        // Counts the work up to `t` in whole steps; the anchor stays at the last whole step.
        private void Settle(string districtId, double t)
        {
            var line = Line(districtId);
            var (n, k) = Pace(districtId);
            if (k == 0 || t <= line.Anchor)
            {
                if (k == 0) line.Anchor = Math.Max(line.Anchor, t);
                return;
            }

            var chunks = Math.Floor((t - line.Anchor) / k);
            if (chunks <= 0) return;
            for (var i = 0; i < k; i++) line.Items[i].WorkMs = Math.Min(line.Items[i].NeedMs, line.Items[i].WorkMs + chunks * n);
            line.Anchor += chunks * k;
        }

        private void Reanchor(string districtId, double now)
        {
            Settle(districtId, now);
            Line(districtId).Anchor = now;
        }

        // An item's work at `now` without counting it in.
        private double WorkAt(string districtId, int index, double now)
        {
            var line = Line(districtId);
            var (n, k) = Pace(districtId);
            var item = line.Items[index];
            if (index >= k || now <= line.Anchor) return item.WorkMs;
            return Math.Min(item.NeedMs, item.WorkMs + Math.Floor((now - line.Anchor) / k) * n);
        }

        // The crew, and how many items it works at once.
        private (int N, int K) Pace(string districtId)
        {
            var n = _crews.Assigned(districtId);
            var items = _state.Lines.TryGetValue(districtId, out var line) ? line.Items.Count : 0;
            return (n, Math.Min(n, items));
        }

        private WorkshopLine Line(string districtId)
        {
            if (!_state.Lines.TryGetValue(districtId, out var line))
            {
                line = new WorkshopLine();
                _state.Lines[districtId] = line;
            }

            return line;
        }

        private static IReadOnlyDictionary<string, double> InputGoods(IGoodDefinition good)
            => string.IsNullOrEmpty(good.InputGood) || good.InputGoodAmount <= 0
                ? new Dictionary<string, double>()
                : new Dictionary<string, double> { [good.InputGood] = good.InputGoodAmount };

        private DistrictState District(string id) => _city.Districts.FirstOrDefault(d => d.Id == id);
    }
}
