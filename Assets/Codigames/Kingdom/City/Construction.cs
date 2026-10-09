using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.City
{
    // Builds, upgrades and moves buildings. A job needs a free builder and is paid when it starts; there is no
    // waiting line and no cancelling. A job finishes at its own moment — live or in a replayed absence — so
    // this is a timed system on the timeline. A plantable (a crop plot) is bought the same way but takes no builder
    // and raises no district: it puts its feature on the ground, and how many of that feature stand is its count.
    public class Construction : ITimedSystem
    {
        public const string DISTRICT_PREFIX = "district";
        private const string JOB_PREFIX = "job";

        private readonly Modifiers.IModifiers _modifiers;
        private readonly CityState _city;
        private readonly ITreasury _treasury;
        private readonly Placement _placement;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IConstructionSettings _settings;
        private readonly IResearchGates _gates;
        private readonly IBonuses _bonuses;
        private readonly IPlanting _planting;
        private readonly Harmony _harmony;
        private readonly Goods.Stockpile _stockpile;

        public Construction(CityState city, ITreasury treasury, Placement placement, ICatalog<IBuildingDefinition> buildings,
            IConstructionSettings settings, IResearchGates gates = null, IBonuses bonuses = null, IPlanting planting = null,
            Harmony harmony = null, Goods.Stockpile stockpile = null, Modifiers.IModifiers modifiers = null)
        {
            _modifiers = modifiers;
            _stockpile = stockpile;
            _harmony = harmony;
            _planting = planting;
            _gates = gates;
            _bonuses = bonuses;
            _city = city;
            _treasury = treasury;
            _placement = placement;
            _buildings = buildings;
            _settings = settings;
        }

        public event Action<DistrictState> DistrictPlaced;
        public event Action<DistrictState> DistrictMoved;
        public event Action<ConstructionJob> JobStarted;
        // A job is about to finish, at its own moment: the district is still what it was.
        public event Action<ConstructionJob, DistrictState> JobCompleting;
        public event Action<ConstructionJob, DistrictState> JobCompleted;
        // A plantable put on the ground: its definition and its cell.
        public event Action<string, Vector2Int> Planted;
        // The city is about to change in a way that may move every building's rates (a building placed, raised or
        // moved: Harmony and neighbours), at that moment; then it has.
        public event Action<double> CityChanging;
        public event Action<double> CityChanged;

        public static bool IsPlantable(IBuildingDefinition building) => building.Production.Plants != null;

        // How many of a kind stand: districts, or for a plantable its feature on the ground.
        public int CountOf(IBuildingDefinition building)
            => IsPlantable(building) ? _planting?.Count(building.Production.Plants) ?? 0 : CityQueries.Count(_city, building.Id);

        public ConstructionRefusal Build(string definitionId, Vector2Int anchor, double now)
        {
            var refusal = BuildRefusal(definitionId);
            if (refusal != ConstructionRefusal.None && refusal != ConstructionRefusal.CannotAfford) return refusal;
            if (_placement.Check(definitionId, anchor) != PlacementProblem.None) return ConstructionRefusal.Placement;

            var building = _buildings.Get(definitionId);
            var ordinal = CountOf(building) + 1;
            if (!_treasury.CanAfford(BuildingPricing.Currencies(building, ordinal, 1))) return ConstructionRefusal.CannotAfford;
            if (!CanAffordGoods(building, 1)) return ConstructionRefusal.NotEnoughGoods;
            _treasury.TryPay(BuildingPricing.Currencies(building, ordinal, 1));
            _stockpile?.TryPay(BuildingPricing.Goods(building, 1));
            if (IsPlantable(building)) return Plant(building, anchor, now);

            CityChanging?.Invoke(now);
            var district = Place(definitionId, ordinal, anchor);

            var rings = CityQueries.DistanceFromTownhall(_city, _buildings, _settings, anchor);
            Start(district, 1, BuildSeconds(building, ordinal - 1, rings), now);
            CityChanged?.Invoke(now);
            return ConstructionRefusal.None;
        }

        // Repairs an abandoned one where it stands: a build at level 1 at the next ordinal's price, with no
        // technology asked and its own wait when it has one (0: a build's).
        public ConstructionRefusal Repair(string definitionId, Vector2Int anchor, double seconds, double now)
        {
            var refusal = RepairRefusal(definitionId);
            if (refusal != ConstructionRefusal.None) return refusal;

            var building = _buildings.Get(definitionId);
            var ordinal = CountOf(building) + 1;
            if (!_treasury.TryPay(BuildingPricing.Currencies(building, ordinal, 1))) return ConstructionRefusal.CannotAfford;
            // A plot's repair is its planting: it grows for its source's growth, not a repair's wait.
            if (IsPlantable(building)) return Plant(building, anchor, now);

            CityChanging?.Invoke(now);
            var district = Place(definitionId, ordinal, anchor);
            var rings = CityQueries.DistanceFromTownhall(_city, _buildings, _settings, anchor);
            Start(district, 1, seconds > 0 ? seconds / BuildSpeed : BuildSeconds(building, ordinal - 1, rings), now);
            CityChanged?.Invoke(now);
            return ConstructionRefusal.None;
        }

        // A repair's build refusal: a builder and room under the cap, and the price.
        public ConstructionRefusal RepairRefusal(string definitionId)
        {
            if (!_buildings.TryGet(definitionId, out var building)) return ConstructionRefusal.NotFound;

            var count = CountOf(building);
            var cap = MaxCount(building);
            if (cap.HasValue && count >= cap.Value) return ConstructionRefusal.AtCap;
            if (!IsPlantable(building) && !CityQueries.HasFreeBuilder(_city)) return ConstructionRefusal.NoFreeBuilder;

            if (!_treasury.CanAfford(BuildingPricing.Currencies(building, count + 1, 1))) return ConstructionRefusal.CannotAfford;
            return HarmonyShort(building, 1) > 0 ? ConstructionRefusal.NeedsHarmony : ConstructionRefusal.None;
        }

        // What stands between the city and one more of a kind, wherever it goes.
        public ConstructionRefusal BuildRefusal(string definitionId)
        {
            if (!_buildings.TryGet(definitionId, out var building)) return ConstructionRefusal.NotFound;
            if (!building.Buildable) return ConstructionRefusal.NotBuildable;
            if (MissingTech(_gates?.DistrictTech(definitionId)) != null) return ConstructionRefusal.NeedsResearch;

            var count = CountOf(building);
            var cap = MaxCount(building);
            if (cap.HasValue && count >= cap.Value) return ConstructionRefusal.AtCap;
            if (!IsPlantable(building) && !CityQueries.HasFreeBuilder(_city)) return ConstructionRefusal.NoFreeBuilder;

            if (!_treasury.CanAfford(BuildingPricing.Currencies(building, count + 1, 1))) return ConstructionRefusal.CannotAfford;
            if (!CanAffordGoods(building, 1)) return ConstructionRefusal.NotEnoughGoods;
            return HarmonyShort(building, 1) > 0 ? ConstructionRefusal.NeedsHarmony : ConstructionRefusal.None;
        }

        // The refined goods a build (level 1) or a level costs, as charged: never multiplied by the instance.
        public IReadOnlyDictionary<string, double> GoodsFor(IBuildingDefinition building, int level)
            => _stockpile?.Priced(BuildingPricing.Goods(building, level)) ?? new Dictionary<string, double>();

        private bool CanAffordGoods(IBuildingDefinition building, int level)
            => _stockpile == null || _stockpile.CanAfford(BuildingPricing.Goods(building, level));

        // How much more Harmony a build (level 1) or a level asks than the city has: 0 when it may go ahead.
        public double HarmonyShort(IBuildingDefinition building, int level, string district = null)
            => _harmony?.ShortBy(building, level, district) ?? 0;

        // One more of a kind, its wait on a plot: the one given, else the one nearest the Townhall.
        public BuildOffer Offer(string definitionId, Vector2Int? at = null)
        {
            var building = _buildings.Get(definitionId);
            var count = CountOf(building);
            var plot = at ?? _placement.Nearest(definitionId);
            var rings = plot.HasValue ? CityQueries.DistanceFromTownhall(_city, _buildings, _settings, plot.Value) : 0;

            return new BuildOffer(definitionId, count + 1, BuildingPricing.Currencies(building, count + 1, 1),
                BuildSeconds(building, count, rings), count, MaxCount(building), BuildRefusal(definitionId),
                MissingTech(_gates?.DistrictTech(definitionId)), GoodsFor(building, 1));
        }

        public ConstructionRefusal Upgrade(string districtId, double now)
        {
            var district = _city.Districts.FirstOrDefault(d => d.Id == districtId);
            if (district == null) return ConstructionRefusal.NotFound;

            var refusal = UpgradeRefusal(district);
            if (refusal != ConstructionRefusal.None) return refusal;

            var building = _buildings.Get(district.DefinitionId);
            var target = district.Level + 1;
            if (!_treasury.CanAfford(BuildingPricing.Currencies(building, district.Ordinal, target))) return ConstructionRefusal.CannotAfford;
            if (!CanAffordGoods(building, target)) return ConstructionRefusal.NotEnoughGoods;
            _treasury.TryPay(BuildingPricing.Currencies(building, district.Ordinal, target));
            _stockpile?.TryPay(BuildingPricing.Goods(building, target));

            CityChanging?.Invoke(now);
            Start(district, target, UpgradeSeconds(building, target), now);
            CityChanged?.Invoke(now);
            return ConstructionRefusal.None;
        }

        // A district's next level, as its card shows it.
        public UpgradeOffer UpgradeOffer(string districtId)
        {
            var district = _city.Districts.First(d => d.Id == districtId);
            var building = _buildings.Get(district.DefinitionId);
            var target = district.Level + 1;

            if (district.Level >= building.MaxLevel)
                return new UpgradeOffer(district.Level, new Dictionary<string, double>(), 0, ConstructionRefusal.MaxLevel, 0);

            var price = BuildingPricing.Currencies(building, district.Ordinal, target);
            // The purses before Harmony: what is short is said first.
            var refusal = UpgradeRefusal(district);
            if (refusal is ConstructionRefusal.None or ConstructionRefusal.NeedsHarmony)
            {
                if (!_treasury.CanAfford(price)) refusal = ConstructionRefusal.CannotAfford;
                else if (!CanAffordGoods(building, target)) refusal = ConstructionRefusal.NotEnoughGoods;
            }

            var gateIndex = district.Level - 1;
            var townhall = At(building.Gates.RequiredTownhallLevelPerLevel, gateIndex);
            var population = At(building.Gates.RequiredPopulationPerLevel, gateIndex);

            return new UpgradeOffer(target, price, UpgradeSeconds(building, target), refusal, townhall, population,
                MissingTech(_gates?.LevelTech(district.DefinitionId, target)), GoodsFor(building, target));
        }

        // Every gate on a district's next level, met and unmet alike — a list with ticks is a plan, where the first
        // unmet one alone answered only "why not?". The price is not here: it is what the button spends.
        public IReadOnlyList<UpgradeRequirement> UpgradeRequirements(string districtId)
        {
            var district = _city.Districts.First(d => d.Id == districtId);
            var building = _buildings.Get(district.DefinitionId);
            var requirements = new List<UpgradeRequirement>();
            if (district.Level >= building.MaxLevel) return requirements;

            var gateIndex = district.Level - 1;
            var townhall = At(building.Gates.RequiredTownhallLevelPerLevel, gateIndex);
            // The Townhall does not gate itself, and level 1 is no gate at all.
            if (townhall > 1 && building.Id != _settings.Townhall.Id)
                requirements.Add(new UpgradeRequirement(RequirementKind.TownhallLevel,
                    CityQueries.TownhallLevel(_city, _settings) >= townhall, townhall));

            var tech = _gates?.LevelTech(district.DefinitionId, district.Level + 1);
            if (tech != null) requirements.Add(new UpgradeRequirement(RequirementKind.Research, _gates.IsOpen(tech), tech: tech));

            var population = At(building.Gates.RequiredPopulationPerLevel, gateIndex);
            if (population > 0)
                requirements.Add(new UpgradeRequirement(RequirementKind.Population, _city.Population >= population, population));

            // Only when the level asks more than this one does.
            var harmony = Harmony.Cost(building, district.Level + 1);
            if (harmony > Harmony.Cost(building, district.Level))
                requirements.Add(new UpgradeRequirement(RequirementKind.Harmony, HarmonyShort(building, district.Level + 1, district.Id) <= 0, (int)harmony));

            return requirements;
        }

        // What stands between a district and its next level, before the price.
        public ConstructionRefusal UpgradeRefusal(DistrictState district)
        {
            var building = _buildings.Get(district.DefinitionId);
            if (!district.Built || _city.Jobs.Any(j => j.DistrictId == district.Id)) return ConstructionRefusal.AlreadyUnderWay;
            if (district.Level >= building.MaxLevel) return ConstructionRefusal.MaxLevel;
            if (MissingTech(_gates?.LevelTech(district.DefinitionId, district.Level + 1)) != null) return ConstructionRefusal.NeedsResearch;

            var gates = building.Gates.RequiredTownhallLevelPerLevel;
            var gateIndex = district.Level - 1;
            if (gateIndex < gates.Count && CityQueries.TownhallLevel(_city, _settings) < gates[gateIndex])
                return ConstructionRefusal.NeedsTownhallLevel;

            var population = building.Gates.RequiredPopulationPerLevel;
            if (gateIndex < population.Count && _city.Population < population[gateIndex])
                return ConstructionRefusal.NeedsPopulation;

            if (!CityQueries.HasFreeBuilder(_city)) return ConstructionRefusal.NoFreeBuilder;
            return HarmonyShort(building, district.Level + 1, district.Id) > 0 ? ConstructionRefusal.NeedsHarmony : ConstructionRefusal.None;
        }

        // How many may stand at the Townhall's level, and one more once the technology that allows it is researched.
        public int? MaxCount(IBuildingDefinition building)
        {
            var cap = CityQueries.MaxCount(building, CityQueries.TownhallLevel(_city, _settings));
            var extra = _gates?.ExtraCountTech(building.Id);
            return cap.HasValue && extra != null && _gates.IsOpen(extra) ? cap + 1 : cap;
        }

        // The builders work faster with every rank of build speed: the wait is divided by it.
        private double BuildSeconds(IBuildingDefinition building, int count, int rings)
            => BuildingDurations.BuildSeconds(building.Duration, count, rings) / BuildSpeed;

        private double UpgradeSeconds(IBuildingDefinition building, int target)
            => BuildingDurations.UpgradeSeconds(building.Duration, target, _settings.LateUpgradeFromLevel) / BuildSpeed;

        // The tree's speed, and a boon's (the Pharaoh's builders): each a multiplier, never below the identity.
        private double BuildSpeed => Math.Max(1, _bonuses.Multiplier(TechStats.BUILD_SPEED))
                                     * Math.Max(1, Modifiers.ModifiersExtensions.Apply(_modifiers, "buildSpeed", 1));

        // The technology still to research, or null when there is none or it is done.
        private string MissingTech(string tech) => tech != null && !_gates.IsOpen(tech) ? tech : null;

        private static int At(System.Collections.Generic.IReadOnlyList<int> list, int index) => index < list.Count ? list[index] : 0;

        // Takes `seconds` off a job at `now` (a speed-up): it never ends before `now`, and what a bigger cut leaves over
        // is lost. A job that ends finishes at once.
        public bool Hurry(string jobId, double seconds, double now)
        {
            var job = _city.Jobs.FirstOrDefault(j => j.Id == jobId);
            if (job == null) return false;
            job.Seconds = Math.Max((now - job.StartedAt) / 1000, job.Seconds - seconds);
            if (job.CompletesAt <= now) ApplyDue(now);
            return true;
        }

        // Seconds left on a job at `now`; null when there is no such job.
        public double? RemainingSeconds(string jobId, double now)
        {
            var job = _city.Jobs.FirstOrDefault(j => j.Id == jobId);
            return job == null ? null : Math.Max(0, (job.CompletesAt - now) / 1000);
        }

        // Free and instant; an unfinished building moves too, keeping its place in the work and its wait.
        public ConstructionRefusal Move(string districtId, Vector2Int anchor, double now)
        {
            var district = _city.Districts.FirstOrDefault(d => d.Id == districtId);
            if (district == null) return ConstructionRefusal.NotFound;
            if (!_buildings.Get(district.DefinitionId).Buildable) return ConstructionRefusal.NotBuildable;
            if (anchor == district.Anchor) return ConstructionRefusal.None;
            if (_placement.Check(district.DefinitionId, anchor, district.Id) != PlacementProblem.None) return ConstructionRefusal.Placement;

            CityChanging?.Invoke(now);
            district.Anchor = anchor;
            CityChanged?.Invoke(now);
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
                // Completing while the job still stands: the city is still what it was.
                var district = _city.Districts.First(d => d.Id == job.DistrictId);
                JobCompleting?.Invoke(job, district);
                _city.Jobs.Remove(job);
                district.Level = job.TargetLevel;
                district.Built = true;
                JobCompleted?.Invoke(job, district);
            }
        }

        public void RunUntil(double time)
        {
        }

        private ConstructionRefusal Plant(IBuildingDefinition building, Vector2Int anchor, double now)
        {
            _planting?.Plant(building.Production.Plants, anchor, now);
            Planted?.Invoke(building.Id, anchor);
            return ConstructionRefusal.None;
        }

        private DistrictState Place(string definitionId, int ordinal, Vector2Int anchor)
        {
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
            return district;
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
