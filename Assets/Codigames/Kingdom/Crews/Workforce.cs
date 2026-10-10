using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.Crews
{
    // Villagers working for a building: each walks to the nearest unclaimed cell of what the building works inside
    // its reach, strikes it once, walks the load home into the building's store, and goes out again. Nobody sets
    // out while the store has no room; a load already on its way lands whole. An emptied cell sends its worker to
    // another, or to wait by the door until one grows back.
    //
    // Every step happens at its own absolute moment, processed in order inside the advance, so a long absence and
    // live play end in the same state without a timeline boundary per step.
    public class Workforce : ITimedSystem
    {
        private const string WORKER_PREFIX = "worker";
        private const double MIN_STRIKE_MS = 100;

        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IRevealedGround _revealed;
        private readonly Harvesting _harvesting;
        private readonly Stores _stores;
        private readonly IWorkerSettings _settings;
        private readonly IBonuses _bonuses;
        private readonly Lairs.ILairGround _lairs;

        private readonly Relics.IRelicAura _aura;

        public Workforce(CityState city, ICatalog<IBuildingDefinition> buildings, IRevealedGround revealed, Harvesting harvesting,
            Stores stores, IWorkerSettings settings, IBonuses bonuses = null, Lairs.ILairGround lairs = null, Relics.IRelicAura aura = null)
        {
            _aura = aura;
            _lairs = lairs;
            _bonuses = bonuses;
            _city = city;
            _buildings = buildings;
            _revealed = revealed;
            _harvesting = harvesting;
            _stores = stores;
            _settings = settings;

            // A store emptied lets its waiting crew out again from that moment.
            _stores.Collected += (district, moved, now) => WakeIdle(district.Id, now);
        }

        // A strike landed on a cell: the cell and what it yields.
        public event Action<WorkerState, Vector2Int> Struck;

        // Villagers not working for any building.
        public int FreeVillagers => Math.Max(0, _city.Population - _city.Workers.Count);

        public IEnumerable<WorkerState> WorkersOf(string districtId) => _city.Workers.Where(w => w.BuildingId == districtId);

        public int Assigned(string districtId) => _city.Workers.Count(w => w.BuildingId == districtId);

        // Whole workers and whole tiles: the tree's flat ranks, aimed at the building or not, rounded down.
        public int Limit(DistrictState district) => Raised(TechStats.CREW_SLOTS, Production(district).MaxWorkersPerLevel, district);

        public int Radius(DistrictState district) => Raised(TechStats.INFLUENCE_RADIUS, Production(district).InfluenceRadiusPerLevel, district);

        // What the crews bring home of a currency a second, nominally: each worker's walk out and back across its
        // building's reach and its swing, a building with several sources splitting its crew between them.
        public double GatherPerSecond(string currency)
            => _city.Districts.Where(d => d.Built).Sum(d => GatherPerSecond(d, currency));

        // What one building's crew brings home of a currency a second (its card's output tile).
        public double GatherPerSecond(DistrictState district, string currency)
        {
            var crew = Assigned(district.Id);
            var sources = Production(district).HarvestSources;
            var source = sources.Select(_harvesting.Source).FirstOrDefault(s => s != null && s.Currency == currency);
            if (!district.Built || crew == 0 || source == null) return 0;

            var cycleSeconds = 2 * Radius(district) / WalkSpeed(district) + StrikeMs(source, district) / 1000;
            return cycleSeconds > 0 ? (double)crew / sources.Count * Delivery(source, district) / cycleSeconds : 0;
        }

        // The currencies a building's crew brings home, in the order its sources name them.
        public IReadOnlyList<string> Currencies(DistrictState district)
            => Production(district).HarvestSources.Select(_harvesting.Source).Where(s => s != null).Select(s => s.Currency).Distinct().ToList();

        // A crew of its own: one that harvests, or a workshop's at the bench.
        public bool HasCrew(DistrictState district)
            => Production(district).HarvestSources.Count > 0 || !string.IsNullOrEmpty(Production(district).Produces);

        // A building's crew is about to change: its id and the moment.
        public event Action<string, double> CrewChanging;

        public bool CanAssign(DistrictState district)
            => district.Built && HasCrew(district) && FreeVillagers > 0 && Assigned(district.Id) < Limit(district);

        public bool Assign(string districtId, double now)
        {
            var district = District(districtId);
            if (district == null || !CanAssign(district)) return false;

            CrewChanging?.Invoke(districtId, now);
            var worker = new WorkerState { Id = _city.NewId(WORKER_PREFIX), BuildingId = districtId, StateStartedAt = now };
            _city.Workers.Add(worker);
            Dispatch(worker, district, now);
            return true;
        }

        // Sends a villager home: one waiting by the door first, else the last to go out (its load is lost).
        public bool Unassign(string districtId, double now)
        {
            var crew = WorkersOf(districtId).ToList();
            if (crew.Count == 0) return false;

            CrewChanging?.Invoke(districtId, now);
            _city.Workers.Remove(crew.FirstOrDefault(w => w.Activity == WorkerActivity.Idle) ?? crew[crew.Count - 1]);
            return true;
        }

        // The ground a building's crew reaches: its footprint and every cell within its radius of it.
        public IReadOnlyList<Vector2Int> Reach(DistrictState district)
        {
            var building = _buildings.Get(district.DefinitionId);
            return GridMath.AroundRect(district.Anchor, building.Width, building.Height, Radius(district)).ToList();
        }

        // The cells a building's crew may work, nearest first: revealed, holding what it works, inside its reach, on no
        // lair's ground — a haul already on its way lands, but nobody walks out to the zone again.
        public IReadOnlyList<Vector2Int> Workable(DistrictState district)
        {
            var building = _buildings.Get(district.DefinitionId);
            var sources = building.Production.HarvestSources;
            if (sources.Count == 0) return Array.Empty<Vector2Int>();

            var footprint = new HashSet<Vector2Int>(GridMath.Rect(district.Anchor, building.Width, building.Height));
            return GridMath.AroundRect(district.Anchor, building.Width, building.Height, Radius(district))
                .Where(c => !footprint.Contains(c) && _revealed.IsRevealed(c) && Works(sources, c) && _lairs?.HoldingAt(c) == null)
                .OrderBy(c => GridMath.Euclidean(c, district.Anchor)).ThenBy(c => c.Y).ThenBy(c => c.X)
                .ToList();
        }

        public double? NextBoundary(double after) => null;

        public void ApplyDue(double time) { }

        // Every worker step due by `time`, earliest first.
        public void RunUntil(double time)
        {
            while (true)
            {
                WorkerState next = null;
                DistrictState nextBuilding = null;
                var nextAt = double.PositiveInfinity;

                foreach (var worker in _city.Workers)
                {
                    var building = District(worker.BuildingId);
                    if (building == null || !building.Built) continue;

                    var at = NextEventAt(worker, building);
                    if (at.HasValue && at.Value <= time && at.Value < nextAt)
                    {
                        next = worker;
                        nextBuilding = building;
                        nextAt = at.Value;
                    }
                }

                if (next == null) return;

                var before = next.Activity;
                Step(next, nextBuilding, nextAt);

                // A worker that looked and found nothing looks again only after this moment.
                if (before == WorkerActivity.Idle && next.Activity == WorkerActivity.Idle) next.StateStartedAt = nextAt + 1;
            }
        }

        // Waiting workers look again from `now`: a cell came back, a claim was freed, the store was emptied.
        public void WakeIdle(string districtId, double now)
        {
            foreach (var worker in WorkersOf(districtId))
            {
                if (worker.Activity == WorkerActivity.Idle) worker.StateStartedAt = Math.Max(worker.StateStartedAt, now);
            }
        }

        // Where a worker is between its building and its cell, 0 at the door and 1 at the cell.
        public double Progress(WorkerState worker, double now)
        {
            if (worker.StateUntil == null || worker.StateUntil <= worker.StateStartedAt) return worker.Activity == WorkerActivity.Working ? 1 : 0;

            var t = Math.Min(1, Math.Max(0, (now - worker.StateStartedAt) / (worker.StateUntil.Value - worker.StateStartedAt)));
            return worker.Activity switch
            {
                WorkerActivity.MovingToCell => t,
                WorkerActivity.MovingHome => 1 - t,
                WorkerActivity.Working => 1,
                _ => 0,
            };
        }

        private double? NextEventAt(WorkerState worker, DistrictState building)
        {
            if (worker.Activity != WorkerActivity.Idle) return worker.StateUntil;
            if (_stores.IsFull(building, worker.StateStartedAt)) return null;

            // The earliest any workable cell is free: the same cells as Workable, walked without building the list (an
            // idle crew is asked this every frame).
            var definition = _buildings.Get(building.DefinitionId);
            var sources = definition.Production.HarvestSources;
            if (sources.Count == 0) return null;
            var radius = Radius(building);
            double? earliest = null;
            for (var x = building.Anchor.X - radius; x < building.Anchor.X + definition.Width + radius; x++)
            for (var y = building.Anchor.Y - radius; y < building.Anchor.Y + definition.Height + radius; y++)
            {
                if (x >= building.Anchor.X && x < building.Anchor.X + definition.Width && y >= building.Anchor.Y && y < building.Anchor.Y + definition.Height)
                    continue;
                var cell = new Vector2Int(x, y);
                if (!_revealed.IsRevealed(cell) || !Works(sources, cell) || _lairs?.HoldingAt(cell) != null || ClaimedByOther(cell, worker)) continue;

                var recovers = _harvesting.RecoversAt(cell);
                var at = Math.Max(worker.StateStartedAt, recovers ?? worker.StateStartedAt);
                if (earliest == null || at < earliest) earliest = at;
            }

            return earliest;
        }

        private void Step(WorkerState worker, DistrictState building, double t)
        {
            var sources = Production(building).HarvestSources;

            switch (worker.Activity)
            {
                case WorkerActivity.Idle:
                    Dispatch(worker, building, t);
                    break;

                case WorkerActivity.MovingToCell:
                {
                    var cell = worker.ClaimedCell.Value;
                    if (_harvesting.IsExhausted(cell, t) || !Works(sources, cell))
                    {
                        worker.ClaimedCell = null;
                        Set(worker, WorkerActivity.MovingHome, t, t + WalkMs(cell, building));
                    }
                    else
                    {
                        Set(worker, WorkerActivity.Working, t, t + StrikeMs(_harvesting.SourceAt(cell), building));
                    }

                    break;
                }

                case WorkerActivity.Working:
                {
                    var cell = worker.ClaimedCell.Value;
                    var source = _harvesting.SourceAt(cell);
                    if (source == null || !Works(sources, cell))
                    {
                        worker.ClaimedCell = null;
                        Set(worker, WorkerActivity.MovingHome, t, t + WalkMs(cell, building));
                        break;
                    }

                    var owed = Delivery(source, building, cell) + worker.StrikeCarry;
                    var want = Math.Floor(owed + 1e-9);
                    worker.StrikeCarry = Math.Max(0, owed - want);
                    worker.Carrying = _harvesting.Draw(cell, want, t);
                    worker.CarriedCurrency = worker.Carrying > 0 ? source.Currency : null;
                    if (worker.Carrying > 0) Struck?.Invoke(worker, cell);

                    Set(worker, WorkerActivity.MovingHome, t, t + WalkMs(cell, building));
                    break;
                }

                case WorkerActivity.MovingHome:
                {
                    if (worker.Carrying > 0 && worker.CarriedCurrency != null) _stores.Deposit(building, worker.CarriedCurrency, worker.Carrying);
                    worker.Carrying = 0;
                    worker.CarriedCurrency = null;

                    var cell = worker.ClaimedCell;
                    if (cell.HasValue && !_stores.IsFull(building, t) && !_harvesting.IsExhausted(cell.Value, t) && Works(sources, cell.Value))
                    {
                        Set(worker, WorkerActivity.MovingToCell, t, t + WalkMs(cell.Value, building));
                    }
                    else
                    {
                        worker.ClaimedCell = null;
                        Dispatch(worker, building, t);
                    }

                    break;
                }
            }
        }

        // From the door: claim the nearest free cell and head out, or wait.
        private void Dispatch(WorkerState worker, DistrictState building, double at)
        {
            var cell = _stores.IsFull(building, at)
                ? (Vector2Int?)null
                : Workable(building).Where(c => !ClaimedByOther(c, worker) && !_harvesting.IsExhausted(c, at)).Select(c => (Vector2Int?)c).FirstOrDefault();

            if (cell.HasValue)
            {
                worker.ClaimedCell = cell;
                Set(worker, WorkerActivity.MovingToCell, at, at + WalkMs(cell.Value, building));
            }
            else
            {
                worker.ClaimedCell = null;
                Set(worker, WorkerActivity.Idle, at, null);
            }
        }

        private bool ClaimedByOther(Vector2Int cell, WorkerState worker)
        {
            foreach (var other in _city.Workers)
                if (other != worker && other.ClaimedCell == cell) return true;
            return false;
        }

        private bool Works(IReadOnlyList<string> sources, Vector2Int cell)
        {
            var source = _harvesting.SourceAt(cell);
            if (source == null || _harvesting.MissingTech(source) != null) return false;
            for (var i = 0; i < sources.Count; i++)
                if (sources[i] == source.Id) return true;
            return false;
        }

        private double WalkMs(Vector2Int cell, DistrictState building) => GridMath.Euclidean(cell, building.Anchor) / WalkSpeed(building) * 1000;

        // The Winged Hammer's aura over a crew's building quickens its walk and its swing.
        private double WalkSpeed(DistrictState building)
            => Math.Max(0.1, _settings.MoveSpeedTilesPerSecond * _bonuses.Multiplier(TechStats.WORKER_SPEED)
                             * Relics.RelicAuraExtensions.Over(_aura, Relics.RelicStats.WORKER_SPEED, building));

        // Units one strike brings home: the ground's yield and the building's late levels, raised by the crews' yield.
        private double Delivery(IHarvestSource source, DistrictState building, Vector2Int? cell = null)
            => ((cell.HasValue ? _harvesting.UnitsPerStrike(source, cell.Value) : _harvesting.UnitsPerStrike(source)) + At(Production(building).ExtraUnitsPerDeliveryPerLevel, building.Level))
               * _bonuses.Multiplier(TechStats.CREW_YIELD);

        private double StrikeMs(IHarvestSource source, DistrictState building)
        {
            var speed = Math.Max(0.01, At(Production(building).StrikeSpeedPerLevel, building.Level, 1))
                        * Math.Max(1, _bonuses.Multiplier(TechStats.CREW_STRIKE_SPEED, TargetKind.District, building.DefinitionId))
                        * Relics.RelicAuraExtensions.Over(_aura, Relics.RelicStats.WORKER_STRIKE_SPEED, building);
            return Math.Max(MIN_STRIKE_MS, Math.Round(source.SecondsPerStrike * 1000 / speed));
        }

        private static void Set(WorkerState worker, WorkerActivity activity, double at, double? until)
        {
            worker.Activity = activity;
            worker.StateStartedAt = at;
            worker.StateUntil = until;
        }

        private int Raised(string stat, IReadOnlyList<int> perLevel, DistrictState district)
        {
            var level = At(perLevel, district.Level);
            return level <= 0 ? 0 : (int)Math.Floor(_bonuses.Apply(stat, level, TargetKind.District, district.DefinitionId));
        }

        private DistrictState District(string id)
        {
            foreach (var district in _city.Districts)
                if (district.Id == id) return district;
            return null;
        }

        private IBuildingProduction Production(DistrictState district) => _buildings.Get(district.DefinitionId).Production;

        private static int At(IReadOnlyList<int> perLevel, int level)
            => perLevel.Count == 0 ? 0 : perLevel[Math.Min(Math.Max(level, 1), perLevel.Count) - 1];

        private static double At(IReadOnlyList<double> perLevel, int level, double blank = 0)
            => perLevel.Count == 0 ? blank : perLevel[Math.Min(Math.Max(level, 1), perLevel.Count) - 1];
    }
}
