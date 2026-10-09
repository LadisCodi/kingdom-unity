using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.City
{
    // The city's Harmony (Docs/features/21-harmony.md): what its decorations supply standing, against what its
    // buildings demand at the levels they hold or are going up to. A fact read off the city, never stored. It gates
    // the next build or level, never takes back what stands, and a surplus raises the rent at its base.
    public class Harmony
    {
        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IHarmonySettings _settings;
        private readonly IBonuses _bonuses;

        public Harmony(CityState city, ICatalog<IBuildingDefinition> buildings, IHarmonySettings settings, IBonuses bonuses = null)
        {
            _city = city;
            _buildings = buildings;
            _settings = settings;
            _bonuses = bonuses;
        }

        public static bool IsDecoration(IBuildingDefinition building) => building.Production.HarmonySupply > 0;

        // A building's demand at a level: a total, not a step (entry 0 is what building it asks).
        public static double Cost(IBuildingDefinition building, int level)
        {
            var costs = building.Production.HarmonyCostPerLevel;
            return costs.Count == 0 ? 0 : costs[Math.Min(Math.Max(level, 1), costs.Count) - 1];
        }

        // What a decoration supplies standing, raised by the tree.
        public double SupplyOf(DistrictState district)
        {
            var supply = _buildings.Get(district.DefinitionId).Production.HarmonySupply;
            if (supply <= 0) return 0;
            return Math.Floor(_bonuses?.Apply(TechStats.DECORATION_HARMONY, supply, TargetKind.District, district.DefinitionId) ?? supply);
        }

        // Only what stands supplies: a piece going up gives nothing yet.
        public double Supply => _city.Districts.Where(d => d.Built).Sum(SupplyOf);

        // Every building's demand, one going up or being raised counted at the level it is going to; `exclude` leaves
        // one out.
        public double Demand(string exclude = null)
            => _city.Districts.Where(d => d.Id != exclude).Sum(d => Cost(_buildings.Get(d.DefinitionId), DemandLevel(d)));

        // How much more Harmony a build (level 1) or a level asks than the city has; 0 when it may go ahead. A level
        // replaces its building's demand rather than adding to it.
        public double ShortBy(IBuildingDefinition building, int level, string district = null)
        {
            var cost = Cost(building, level);
            if (cost <= 0) return 0;
            return Math.Max(0, Demand(district) + cost - Supply);
        }

        // The last surplus tier the city reaches; none for a city that demands nothing.
        public HarmonyTier? Tier
        {
            get
            {
                var demand = Demand();
                if (demand <= 0) return null;

                var ratio = Supply / demand;
                HarmonyTier? reached = null;
                foreach (var tier in _settings.SurplusTiers)
                    if (ratio >= tier.At) reached = tier;
                return reached;
            }
        }

        // What the surplus does to the rent.
        public double Multiplier => 1 + (Tier?.Bonus ?? 0);

        private int DemandLevel(DistrictState district)
        {
            var job = _city.Jobs.FirstOrDefault(j => j.DistrictId == district.Id);
            return job != null ? Math.Max(job.TargetLevel, district.Level) : district.Level;
        }
    }
}
