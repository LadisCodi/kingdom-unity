using System;
using System.Linq;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Core;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.City
{
    // Builds, upgrades and moves buildings. A job needs a free builder and is paid when it starts; there is no
    // waiting line and no cancelling. A job finishes at its own moment — live or in a replayed absence — so
    // this is a timed system on the timeline.
    public class Construction : ITimedSystem
    {
        public const string DISTRICT_PREFIX = "district";
        private const string JOB_PREFIX = "job";

        private readonly CityState _city;
        private readonly ITreasury _treasury;
        private readonly Placement _placement;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IConstructionSettings _settings;

        public Construction(CityState city, ITreasury treasury, Placement placement, ICatalog<IBuildingDefinition> buildings,
            IConstructionSettings settings)
        {
            _city = city;
            _treasury = treasury;
            _placement = placement;
            _buildings = buildings;
            _settings = settings;
        }

        public event Action<DistrictState> DistrictPlaced;
        public event Action<DistrictState> DistrictMoved;
        public event Action<ConstructionJob> JobStarted;
        public event Action<ConstructionJob, DistrictState> JobCompleted;

        public ConstructionRefusal Build(string definitionId, Vector2Int anchor, double now)
        {
            var refusal = BuildRefusal(definitionId);
            if (refusal != ConstructionRefusal.None && refusal != ConstructionRefusal.CannotAfford) return refusal;
            if (_placement.Check(definitionId, anchor) != PlacementProblem.None) return ConstructionRefusal.Placement;

            var building = _buildings.Get(definitionId);
            var ordinal = CityQueries.Count(_city, definitionId) + 1;
            if (!_treasury.TryPay(BuildingPricing.Currencies(building, ordinal, 1))) return ConstructionRefusal.CannotAfford;

            var district = new DistrictState
            {
                Id = _city.NewId(DISTRICT_PREFIX),
                DefinitionId = definitionId,
                Ordinal = ordinal,
                Level = 1,
                Anchor = anchor,
                Built = false,
            };
            _city.Districts.Add(district);
            DistrictPlaced?.Invoke(district);

            var rings = CityQueries.DistanceFromTownhall(_city, _buildings, _settings, anchor);
            Start(district, 1, BuildingDurations.BuildSeconds(building.Duration, ordinal - 1, rings), now);
            return ConstructionRefusal.None;
        }

        // What stands between the city and one more of a kind, wherever it goes.
        public ConstructionRefusal BuildRefusal(string definitionId)
        {
            if (!_buildings.TryGet(definitionId, out var building)) return ConstructionRefusal.NotFound;
            if (!building.Buildable) return ConstructionRefusal.NotBuildable;

            var count = CityQueries.Count(_city, definitionId);
            var cap = CityQueries.MaxCount(building, CityQueries.TownhallLevel(_city, _settings));
            if (cap.HasValue && count >= cap.Value) return ConstructionRefusal.AtCap;
            if (!CityQueries.HasFreeBuilder(_city)) return ConstructionRefusal.NoFreeBuilder;

            return _treasury.CanAfford(BuildingPricing.Currencies(building, count + 1, 1))
                ? ConstructionRefusal.None
                : ConstructionRefusal.CannotAfford;
        }

        // One more of a kind, its wait on a plot: the one given, else the one nearest the Townhall.
        public BuildOffer Offer(string definitionId, Vector2Int? at = null)
        {
            var building = _buildings.Get(definitionId);
            var count = CityQueries.Count(_city, definitionId);
            var plot = at ?? _placement.Nearest(definitionId);
            var rings = plot.HasValue ? CityQueries.DistanceFromTownhall(_city, _buildings, _settings, plot.Value) : 0;

            return new BuildOffer(definitionId, count + 1, BuildingPricing.Currencies(building, count + 1, 1),
                BuildingDurations.BuildSeconds(building.Duration, count, rings), count,
                CityQueries.MaxCount(building, CityQueries.TownhallLevel(_city, _settings)), BuildRefusal(definitionId));
        }

        public ConstructionRefusal Upgrade(string districtId, double now)
        {
            var district = _city.Districts.FirstOrDefault(d => d.Id == districtId);
            if (district == null) return ConstructionRefusal.NotFound;

            var refusal = UpgradeRefusal(district);
            if (refusal != ConstructionRefusal.None) return refusal;

            var building = _buildings.Get(district.DefinitionId);
            var target = district.Level + 1;
            if (!_treasury.TryPay(BuildingPricing.Currencies(building, district.Ordinal, target))) return ConstructionRefusal.CannotAfford;

            Start(district, target, BuildingDurations.UpgradeSeconds(building.Duration, target, _settings.LateUpgradeFromLevel), now);
            return ConstructionRefusal.None;
        }

        // What stands between a district and its next level, before the price.
        public ConstructionRefusal UpgradeRefusal(DistrictState district)
        {
            var building = _buildings.Get(district.DefinitionId);
            if (!district.Built || _city.Jobs.Any(j => j.DistrictId == district.Id)) return ConstructionRefusal.AlreadyUnderWay;
            if (district.Level >= building.MaxLevel) return ConstructionRefusal.MaxLevel;

            var gates = building.Gates.RequiredTownhallLevelPerLevel;
            var gateIndex = district.Level - 1;
            if (gateIndex < gates.Count && CityQueries.TownhallLevel(_city, _settings) < gates[gateIndex])
                return ConstructionRefusal.NeedsTownhallLevel;

            return CityQueries.HasFreeBuilder(_city) ? ConstructionRefusal.None : ConstructionRefusal.NoFreeBuilder;
        }

        // Free and instant; an unfinished building moves too, keeping its place in the work and its wait.
        public ConstructionRefusal Move(string districtId, Vector2Int anchor)
        {
            var district = _city.Districts.FirstOrDefault(d => d.Id == districtId);
            if (district == null) return ConstructionRefusal.NotFound;
            if (!_buildings.Get(district.DefinitionId).Buildable) return ConstructionRefusal.NotBuildable;
            if (anchor == district.Anchor) return ConstructionRefusal.None;
            if (_placement.Check(district.DefinitionId, anchor, district.Id) != PlacementProblem.None) return ConstructionRefusal.Placement;

            district.Anchor = anchor;
            DistrictMoved?.Invoke(district);
            return ConstructionRefusal.None;
        }

        public double? NextBoundary(double after)
        {
            double? earliest = null;
            foreach (var job in _city.Jobs)
            {
                if (job.CompletesAt > after && (!earliest.HasValue || job.CompletesAt < earliest.Value)) earliest = job.CompletesAt;
            }

            return earliest;
        }

        public void ApplyDue(double time)
        {
            foreach (var job in _city.Jobs.Where(j => j.CompletesAt <= time).OrderBy(j => j.CompletesAt).ToList())
            {
                _city.Jobs.Remove(job);

                var district = _city.Districts.First(d => d.Id == job.DistrictId);
                district.Level = job.TargetLevel;
                district.Built = true;
                JobCompleted?.Invoke(job, district);
            }
        }

        public void RunUntil(double time)
        {
        }

        private void Start(DistrictState district, int targetLevel, double seconds, double now)
        {
            var job = new ConstructionJob
            {
                Id = _city.NewId(JOB_PREFIX),
                DistrictId = district.Id,
                TargetLevel = targetLevel,
                StartedAt = now,
                Seconds = seconds,
            };
            _city.Jobs.Add(job);
            JobStarted?.Invoke(job);
        }
    }
}
