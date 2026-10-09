using System.Collections.Generic;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Harvest.State;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Magic.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Tests.Builders
{
    // The city fixture's province with its ground worked by hand: trees that grow back, berry bushes that are
    // eaten and come back elsewhere, rocks that never run out; a tap is ten seconds of work for one Mana; the
    // pool holds 100 and gains 12 an hour.
    public class HarvestFixture : CityFixture
    {
        public static readonly Vector2Int TREE = new(3, 3);
        public static readonly Vector2Int BUSH = new(-3, 3);
        public static readonly Vector2Int ROCK = new(3, -3);

        public HarvestFixture(double grasslandWood = 1)
        {
            Yields = new FakeYields { ["Grassland/Wood"] = grasslandWood };
            Sources = new Catalog<IHarvestSource>(new IHarvestSource[]
            {
                new FakeSource { Id = "Forest", Currency = "Wood", UnitsPerStrike = 1, SecondsPerStrike = 10, Stock = 10, RecoverySeconds = 180 },
                new FakeSource { Id = "Crops", Currency = "Food", UnitsPerStrike = 1, SecondsPerStrike = 8, Stock = 10, RecoverySeconds = 60 },
                new FakeSource { Id = "Berries", Currency = "Food", UnitsPerStrike = 1, SecondsPerStrike = 10, Stock = 10, RespawnSeconds = 120 },
                new FakeSource { Id = "Stone", Currency = "Stone", UnitsPerStrike = 1, SecondsPerStrike = 26 },
            });
            Features = new Catalog<IFeatureDefinition>(new IFeatureDefinition[]
            {
                new FakeFeature { Id = "Trees", Source = "Forest", RespawnTerrain = "Grassland" },
                new FakeFeature { Id = "Crops", Source = "Crops", RespawnTerrain = "Grassland" },
                new FakeFeature { Id = "BerryBush", Source = "Berries", RespawnTerrain = "Grassland" },
                new FakeFeature { Id = "Mountain", Source = "Stone", RespawnTerrain = "Grassland" },
            });

            Ground.Features[TREE] = "Trees";
            Ground.Features[BUSH] = "BerryBush";
            Ground.Features[ROCK] = "Mountain";

            Mana = new ManaPool(ManaState, Treasury, new FakeMana());
            Harvesting = new Harvesting(HarvestState, Ground, City, Map, Buildings, Features, Sources, Yields,
                new FakeTap(), Treasury, Mana, 42, Revealed);
            Timeline.Register(Mana);
            Timeline.Register(Harvesting);
        }

        public HarvestState HarvestState { get; } = new();
        public ManaState ManaState { get; } = new();
        public FakeYields Yields { get; }
        public Catalog<IHarvestSource> Sources { get; }
        public Catalog<IFeatureDefinition> Features { get; }
        public ManaPool Mana { get; }
        public Harvesting Harvesting { get; }

        public sealed class FakeYields : Dictionary<string, double>, ITerrainYields
        {
            public double YieldOf(string terrain, string currency) => TryGetValue(terrain + "/" + currency, out var m) ? m : 1;
        }

        private sealed class FakeSource : IHarvestSource
        {
            public string Id { get; set; }
            public string Currency { get; set; }
            public double UnitsPerStrike { get; set; }
            public double SecondsPerStrike { get; set; }
            public double Stock { get; set; }
            public double RecoverySeconds { get; set; }
            public double RespawnSeconds { get; set; }
        }

        private sealed class FakeFeature : IFeatureDefinition
        {
            public string Id { get; set; }
            public string Source { get; set; }
            public string RespawnTerrain { get; set; }
        }

        private sealed class FakeTap : ITapSettings
        {
            public double WorkSeconds => 10;
            public double ManaCost => 1;
        }

        private sealed class FakeMana : IManaSettings
        {
            public double BaseCap => 100;
            public double BasePerHour => 12;
        }
    }
}
