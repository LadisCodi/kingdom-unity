using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Army.State;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.Army
{
    // The city's army (Docs/features/combat.md §4, §14; buildings.md §4.8–§4.9). Soldiers are trained one at a time in
    // a hall's line, paid for when queued; the head of a line is priced when it starts — its neighbours and the tree —
    // and the next starts the moment it finishes, so a replayed absence trains in the same order as a watched one.
    // The halls' levels set the army's cap, and what is queued counts against it as soldiers already do. Soldiers who
    // fall in a fight may reach a bed in the Infirmary and be mended there, in its own line, at a share of their price.
    public class Army : ITimedSystem
    {
        private readonly ArmyState _state;
        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly ICatalog<IUnitDefinition> _units;
        private readonly ITreasury _treasury;
        private readonly IArmySettings _settings;
        private readonly IRushSettings _rush;
        private readonly IResearchGates _gates;
        private readonly IBonuses _bonuses;
        private readonly Adjacency _adjacency;

        private readonly Relics.IRelicAura _aura;

        public Army(ArmyState state, CityState city, ICatalog<IBuildingDefinition> buildings, ICatalog<IUnitDefinition> units,
            ITreasury treasury, IArmySettings settings, IRushSettings rush, IResearchGates gates = null, IBonuses bonuses = null,
            Adjacency adjacency = null, Relics.IRelicAura aura = null)
        {
            _aura = aura;
            _state = state;
            _city = city;
            _buildings = buildings;
            _units = units;
            _treasury = treasury;
            _settings = settings;
            _rush = rush;
            _gates = gates;
            _bonuses = bonuses;
            _adjacency = adjacency;
        }

        // A soldier left a hall's line for the army: the hall, the troop, when.
        public event Action<DistrictState, string, double> Trained;

        // A hall's line ran dry with its last soldier: the hall, the troop, when. Mending is not training.
        public event Action<DistrictState, string, double> LineDone;

        // The army or a line changed.
        public event Action Changed;

        // ---- the roster

        public int Count(string troop) => _state.Troops.TryGetValue(troop, out var n) ? n : 0;
        public int Wounded(string troop) => _state.Wounded.TryGetValue(troop, out var n) ? n : 0;
        public IReadOnlyDictionary<string, int> Roster => _state.Troops;
        public IReadOnlyDictionary<string, int> Ward => _state.Wounded;
        public int Size => _state.Troops.Values.Sum();

        // Soldiers and the soldiers queued for (a mending batch whole): what the cap is measured against.
        public int Committed => Size + _state.Lines.Sum(i => i.Count);

        // Σ over the standing halls of their cap at their level — a total per level — raised by the tree.
        public int Cap
        {
            get
            {
                var cap = _city.Districts.Where(d => d.Built).Sum(d => At(Production(d).ArmyCapPerLevel, d.Level));
                return cap <= 0 ? 0 : (int)Math.Round(cap * (_bonuses?.Multiplier(TechStats.ARMY_CAP) ?? 1));
            }
        }

        public int Beds => (int)Math.Floor(_city.Districts.Where(d => d.Built).Sum(d => At(Production(d).BedsPerLevel, d.Level))
                                           * (_bonuses?.Multiplier(TechStats.INFIRMARY_BEDS) ?? 1));

        public int WoundedCount => _state.Wounded.Values.Sum();

        // ---- the halls

        // A hall trains soldiers; the Townhall's villagers are not a unit.
        public bool IsHall(IBuildingDefinition building) => building.Production.Trains.Any(_units.Contains);
        public static bool IsInfirmary(IBuildingDefinition building) => building.Production.BedsPerLevel.Count > 0;

        public IUnitDefinition UnitOf(string troop) => _units.Get(Troops.UnitOf(troop));
        public UnitRank RankOf(string troop) => UnitOf(troop).Ranks[Math.Min(Troops.RankOf(troop), UnitOf(troop).Ranks.Count) - 1];

        // The troops a hall may train, rank by rank.
        public IReadOnlyList<string> TroopsOf(DistrictState hall)
            => Production(hall).Trains.Where(_units.Contains)
                .SelectMany(u => Enumerable.Range(1, _units.Get(u).Ranks.Count).Select(r => Troops.Of(u, r))).ToList();

        public IReadOnlyList<HallItem> Line(string hallId) => _state.Lines.Where(i => i.BuildingId == hallId).ToList();

        // What opens a troop, in the order it is asked: the unit's technology, the rank's, then the hall's level.
        public ArmyRefusal Gate(string troop, DistrictState hall)
        {
            var unit = Troops.UnitOf(troop);
            if (!_units.Contains(unit)) return ArmyRefusal.NoBuilding;
            if (Missing(_gates?.UnitTech(unit)) || Missing(_gates?.EvolutionTech(unit, Troops.RankOf(troop)))) return ArmyRefusal.TechRequired;
            if (hall == null || !hall.Built || !Production(hall).Trains.Contains(unit)) return ArmyRefusal.NoBuilding;
            return hall.Level < RankOf(troop).MinHallLevel ? ArmyRefusal.HallLevel : ArmyRefusal.None;
        }

        // The technology a troop still waits on; null when none.
        public string TechFor(string troop)
        {
            var unit = Troops.UnitOf(troop);
            var tech = _gates?.UnitTech(unit);
            if (Missing(tech)) return tech;
            tech = _gates?.EvolutionTech(unit, Troops.RankOf(troop));
            return Missing(tech) ? tech : null;
        }

        // A price for `count`: each soldier's, rounded, by currency.
        public IReadOnlyDictionary<string, double> TrainCost(string troop, int count)
        {
            var scale = _bonuses?.Apply(TechStats.RECRUIT_COST, 1) ?? 1;
            return RankOf(troop).Cost.Where(c => c.Value > 0)
                .ToDictionary(c => c.Key, c => Math.Max(1, Prices.RoundPrice(c.Value * scale)) * count);
        }

        // Seconds one takes at a hall, priced now: its neighbours, and the tree.
        public double TrainSeconds(string troop, DistrictState hall)
        {
            // The Winged Hammer's aura over the hall, priced when a trainee's clock starts.
            var speed = Math.Max(1, _bonuses?.Multiplier(TechStats.RECRUIT_SPEED, TargetKind.Unit, Troops.UnitOf(troop)) ?? 1)
                        * (hall == null ? 1 : Relics.RelicAuraExtensions.Over(_aura, Relics.RelicStats.TRAINING_SPEED, hall));
            var neighbours = hall == null ? 1 : _adjacency?.Multiplier(hall, AdjacencyStat.TrainTime) ?? 1;
            return Math.Max(1, Math.Round(RankOf(troop).TrainSeconds * neighbours / speed));
        }

        // How many could be queued now: the room under the cap and the purse, at least one.
        public int Most(string troop)
        {
            var room = Math.Max(0, Cap - Committed);
            var each = TrainCost(troop, 1);
            var afford = each.Count == 0 ? room : each.Min(c => (int)Math.Floor(_treasury.Get(c.Key) / c.Value));
            return Math.Max(1, Math.Min(room, afford));
        }

        public ArmyRefusal Refusal(string hallId, string troop, int count)
        {
            var hall = District(hallId);
            var gate = Gate(troop, hall);
            if (gate != ArmyRefusal.None) return gate;
            if (Line(hallId).Any(i => i.Kind == HallItemKind.Recruit && i.Troop != troop)) return ArmyRefusal.OtherRank;
            if (Committed + count > Cap) return ArmyRefusal.AtCapacity;
            return _treasury.CanAfford(TrainCost(troop, count)) ? ArmyRefusal.None : ArmyRefusal.NotEnoughResources;
        }

        // Queues `count` soldiers, all or none, paid now.
        public ArmyRefusal Train(string hallId, string troop, int count, double now)
        {
            var refusal = Refusal(hallId, troop, count);
            if (refusal != ArmyRefusal.None) return refusal;

            _treasury.TryPay(TrainCost(troop, count));
            var idle = Line(hallId).Count == 0;
            for (var i = 0; i < count; i++)
                _state.Lines.Add(new HallItem { Id = _city.NewId("trainee"), Troop = troop, BuildingId = hallId, Kind = HallItemKind.Recruit });
            if (idle) Start(Line(hallId)[0], now);
            Changed?.Invoke();
            return ArmyRefusal.None;
        }

        // ---- the Infirmary

        public DistrictState Infirmary => _city.Districts.FirstOrDefault(d => d.Built && IsInfirmary(_buildings.Get(d.DefinitionId)));

        public IReadOnlyDictionary<string, double> HealCost(string troop, int count)
            => RankOf(troop).Cost.Where(c => c.Value > 0)
                .ToDictionary(c => c.Key, c => Math.Max(1, Prices.RoundPrice(TrainCost(troop, 1)[c.Key] * count * _settings.HealCostShare)));

        public double HealSeconds(string troop, int count)
        {
            var speed = Math.Max(1, _bonuses?.Multiplier(TechStats.HEAL_SPEED) ?? 1);
            return Math.Max(1, Math.Round(RankOf(troop).TrainSeconds * count * _settings.HealTimeShare / speed));
        }

        public ArmyRefusal HealRefusal(string troop)
        {
            var count = Wounded(troop);
            if (count <= 0) return ArmyRefusal.NoneWounded;
            if (Infirmary == null) return ArmyRefusal.NoBuilding;
            if (Committed + count > Cap) return ArmyRefusal.AtCapacity;
            return _treasury.CanAfford(HealCost(troop, count)) ? ArmyRefusal.None : ArmyRefusal.NotEnoughResources;
        }

        // Mends every wounded soldier of a troop, in the Infirmary's own line: out of the beds now, back on the roster when done.
        public ArmyRefusal Heal(string troop, double now)
        {
            var refusal = HealRefusal(troop);
            if (refusal != ArmyRefusal.None) return refusal;

            var count = Wounded(troop);
            var ward = Infirmary;
            _treasury.TryPay(HealCost(troop, count));
            _state.Wounded[troop] = 0;
            var idle = Line(ward.Id).Count == 0;
            var item = new HallItem { Id = _city.NewId("healing"), Troop = troop, BuildingId = ward.Id, Kind = HallItemKind.Heal, Count = count };
            _state.Lines.Add(item);
            if (idle) Start(item, now);
            Changed?.Invoke();
            return ArmyRefusal.None;
        }

        // Soldiers lost in a fight: each troop's fallen leave the roster, and a share of them, while beds are free,
        // wait in the Infirmary instead of dying. Returns how many reached a bed.
        public int Lose(IReadOnlyDictionary<string, int> fallen, double woundedShare)
        {
            var saved = 0;
            foreach (var (troop, count) in fallen)
            {
                var lost = Math.Min(count, Count(troop));
                if (lost <= 0) continue;
                _state.Troops[troop] = Count(troop) - lost;
                var beds = Math.Max(0, Beds - WoundedCount);
                var wounded = Math.Min(beds, (int)Math.Round(lost * woundedShare));
                if (wounded > 0) _state.Wounded[troop] = Wounded(troop) + wounded;
                saved += wounded;
            }

            Changed?.Invoke();
            return saved;
        }

        public double WoundedShare => _settings.WoundedShare;

        // ---- time

        // Seconds until a line is done: what is left of its head, and the rest priced now.
        public double? RemainingSeconds(string hallId, double now)
        {
            var line = Line(hallId);
            if (line.Count == 0) return null;
            var hall = District(hallId);
            var head = line[0];
            var total = head.StartedAt.HasValue ? Math.Max(0, (head.CompletesAt - now) / 1000) : SecondsFor(head, hall);
            foreach (var item in line.Skip(1)) total += SecondsFor(item, hall);
            return total;
        }

        // The head of a line: 0 to 1.
        public double HeadProgress(string hallId, double now)
        {
            var head = Line(hallId).FirstOrDefault();
            if (head?.StartedAt == null || head.Seconds <= 0) return 0;
            return Math.Min(1, Math.Max(0, (now - head.StartedAt.Value + head.CutMs) / (head.Seconds * 1000)));
        }

        public double RushCost(string hallId, double now) => GemRush.GemsToFinish(RemainingSeconds(hallId, now) ?? 0, _rush.SecondsPerGem);

        // Gems finish the whole line now.
        public ArmyRefusal Rush(string hallId, double now)
        {
            var line = Line(hallId);
            if (line.Count == 0) return ArmyRefusal.NothingTraining;
            if (!_treasury.TryPay(new Dictionary<string, double> { [GemRush.GEMS] = RushCost(hallId, now) })) return ArmyRefusal.NotEnoughGems;

            foreach (var item in line)
            {
                _state.Lines.Remove(item);
                Deliver(item, now);
            }

            Done(District(hallId), line[line.Count - 1], now);
            Changed?.Invoke();
            return ArmyRefusal.None;
        }

        // A speed-up on a line: off its head, the rest spilling onto the next; what is over is lost.
        public bool Cut(string hallId, double seconds, double now)
        {
            var budget = seconds * 1000;
            HallItem last = null;
            while (budget > 0)
            {
                var head = Line(hallId).FirstOrDefault();
                if (head == null) break;
                if (head.StartedAt == null) Start(head, now);
                var left = Math.Max(0, head.CompletesAt - now);
                if (budget < left)
                {
                    head.CutMs += budget;
                    break;
                }

                _state.Lines.Remove(head);
                Deliver(head, now);
                last = head;
                budget -= left;
            }

            var next = Line(hallId).FirstOrDefault();
            if (next != null && next.StartedAt == null) Start(next, now);
            if (last != null && next == null) Done(District(hallId), last, now);
            Changed?.Invoke();
            return true;
        }

        public double? NextBoundary(double after)
        {
            double? earliest = null;
            foreach (var hallId in _state.Lines.Select(i => i.BuildingId).Distinct())
            {
                var head = Line(hallId)[0];
                if (head.StartedAt == null) continue;
                if (head.CompletesAt > after && (earliest == null || head.CompletesAt < earliest)) earliest = head.CompletesAt;
            }

            return earliest;
        }

        // Every head due by `time`, earliest first; the next in its line starts the moment it finishes.
        public void ApplyDue(double time)
        {
            while (true)
            {
                HallItem due = null;
                foreach (var hallId in _state.Lines.Select(i => i.BuildingId).Distinct().ToList())
                {
                    var head = Line(hallId)[0];
                    if (head.StartedAt == null) Start(head, time);
                    if (head.CompletesAt <= time && (due == null || head.CompletesAt < due.CompletesAt)) due = head;
                }

                if (due == null) return;
                var at = due.CompletesAt;
                _state.Lines.Remove(due);
                Deliver(due, at);
                var next = Line(due.BuildingId).FirstOrDefault();
                if (next != null) Start(next, at);
                else Done(District(due.BuildingId), due, at);
                Changed?.Invoke();
            }
        }

        public void RunUntil(double time) { }

        private void Start(HallItem item, double now)
        {
            item.StartedAt = now;
            item.Seconds = SecondsFor(item, District(item.BuildingId));
        }

        private double SecondsFor(HallItem item, DistrictState hall)
            => item.Kind == HallItemKind.Heal ? HealSeconds(item.Troop, item.Count) : TrainSeconds(item.Troop, hall);

        private void Deliver(HallItem item, double at)
        {
            _state.Troops[item.Troop] = Count(item.Troop) + item.Count;
            if (item.Kind == HallItemKind.Recruit && District(item.BuildingId) is { } hall) Trained?.Invoke(hall, item.Troop, at);
        }

        private void Done(DistrictState hall, HallItem last, double at)
        {
            if (hall != null && last.Kind == HallItemKind.Recruit) LineDone?.Invoke(hall, last.Troop, at);
        }

        private bool Missing(string tech) => tech != null && _gates != null && !_gates.IsOpen(tech);

        private IBuildingProduction Production(DistrictState district) => _buildings.Get(district.DefinitionId).Production;

        private DistrictState District(string id) => _city.Districts.FirstOrDefault(d => d.Id == id);

        private static int At(IReadOnlyList<int> perLevel, int level)
            => perLevel.Count == 0 ? 0 : perLevel[Math.Min(Math.Max(level, 1), perLevel.Count) - 1];
    }
}
