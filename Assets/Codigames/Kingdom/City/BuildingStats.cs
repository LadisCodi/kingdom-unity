using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.City
{
    // What a building is worth at a level (the web's upgradeStats.ts), one model for two screens: its card prints
    // this level's figures, the upgrade sheet what the next level adds. A level's gain is always the difference of
    // two reads of the same function, so a figure added here reaches both. Pure in the level it is asked about.
    public class BuildingStats
    {
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IConstructionSettings _settings;
        private readonly IBonuses _bonuses;
        private readonly IFogSettings _fog;
        private readonly ICatalog<IHarvestSource> _sources;
        private readonly VillagerTraining _training;

        public BuildingStats(ICatalog<IBuildingDefinition> buildings, IConstructionSettings settings, IBonuses bonuses = null,
            IFogSettings fog = null, ICatalog<IHarvestSource> sources = null, VillagerTraining training = null)
        {
            _buildings = buildings;
            _settings = settings;
            _bonuses = bonuses;
            _fog = fog;
            _sources = sources;
            _training = training;
        }

        public IReadOnlyList<BuildingStat> At(DistrictState district, int level)
        {
            var building = _buildings.Get(district.DefinitionId);
            var production = building.Production;
            var id = building.Id;
            var stats = new List<BuildingStat>();

            // The beds the tree has added included, as the houses fill: the card reads residents against them.
            if (production.PopulationCapacityPerLevel.Count > 0)
                stats.Add(new BuildingStat(StatKind.Beds, Raised(TechStats.POPULATION_CAPACITY, production.PopulationCapacityPerLevel, level, id)));

            if (production.InfluenceRadiusPerLevel.Count > 0)
            {
                // The map rings the reach while the card is open, and the card's stepper says the crew: the sheet keeps both.
                stats.Add(new BuildingStat(StatKind.Range, Raised(TechStats.INFLUENCE_RADIUS, production.InfluenceRadiusPerLevel, level, id), onCard: false));
                stats.Add(new BuildingStat(StatKind.Crew, Raised(TechStats.CREW_SLOTS, production.MaxWorkersPerLevel, level, id), onCard: false));
            }

            // A crew's haul is the late levels' gift, and the card's output already counts it in.
            if (production.ExtraUnitsPerDeliveryPerLevel.Count > 0)
                stats.Add(new BuildingStat(StatKind.Haul, At(production.ExtraUnitsPerDeliveryPerLevel, level), onCard: false));

            if (production.StrikeSpeedPerLevel.Count > 0)
                stats.Add(new BuildingStat(StatKind.Speed, At(production.StrikeSpeedPerLevel, level, 1)));

            if (production.StorageCapacityPerLevel.Count > 0)
            {
                var capacity = Math.Floor(At(production.StorageCapacityPerLevel, level)
                                          * _bonuses.Multiplier(TechStats.STORAGE_CAPACITY, TargetKind.District, id));
                stats.Add(new BuildingStat(StatKind.Storage, capacity, currency: StoreCurrency(production)));
            }

            // The card's Gold an hour already counts the rent in; the sheet shows what a level adds.
            if (production.TaxBonusPerLevel.Count > 0)
                stats.Add(new BuildingStat(StatKind.Rent, Math.Round(At(production.TaxBonusPerLevel, level) * 100), onCard: false));

            if (id == _settings.Townhall.Id)
            {
                if (production.GoldPerMinutePerLevel.Count > 0)
                    stats.Add(new BuildingStat(StatKind.Income,
                        At(production.GoldPerMinutePerLevel, level) * 60 * _bonuses.Multiplier(TechStats.OWN_GOLD)));
                if (_fog != null && _fog.ReachPerTownhallLevel.Count > 0)
                    stats.Add(new BuildingStat(StatKind.Fog, At(_fog.ReachPerTownhallLevel, level)));
                // What the next villager takes to train: the Townhall trains them.
                if (_training != null)
                    stats.Add(new BuildingStat(StatKind.TrainTime, _training.SecondsAt(_training.NextPlace), lowerIsBetter: true));
            }

            return stats;
        }

        // What the next level moves, and only that: a figure the level leaves alone is not part of what is bought.
        public IReadOnlyList<StatChange> Changes(DistrictState district)
        {
            var after = At(district, district.Level + 1).ToDictionary(s => s.Kind, s => s.Value);
            return At(district, district.Level)
                .Select(s => new StatChange(s, (after.TryGetValue(s.Kind, out var next) ? next : s.Value) - s.Value))
                .Where(c => Math.Abs(c.Delta) > 1e-9)
                .ToList();
        }

        private string StoreCurrency(IBuildingProduction production)
        {
            if (production.HarvestSources.Count == 0 || _sources == null) return "Gold";
            return _sources.TryGet(production.HarvestSources[0], out var source) ? source.Currency : "Gold";
        }

        private int Raised(string stat, IReadOnlyList<int> perLevel, int level, string id)
        {
            var value = At(perLevel, level);
            return value <= 0 ? 0 : (int)Math.Floor(_bonuses.Apply(stat, value, TargetKind.District, id));
        }

        private static int At(IReadOnlyList<int> perLevel, int level)
            => perLevel.Count == 0 ? 0 : perLevel[Math.Min(Math.Max(level, 1), perLevel.Count) - 1];

        private static double At(IReadOnlyList<double> perLevel, int level, double blank = 0)
            => perLevel.Count == 0 ? blank : perLevel[Math.Min(Math.Max(level, 1), perLevel.Count) - 1];
    }
}
