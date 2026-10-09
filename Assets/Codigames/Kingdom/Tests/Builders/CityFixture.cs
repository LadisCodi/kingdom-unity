using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Map;
using Codigames.Modules.Core;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.Tests.Builders
{
    // A small province and a city on it, wired as the game wires them: a square of grass from (−5, −5) to
    // (5, 5), a Townhall of 2 × 2 at the origin, one builder, Gold to spend.
    public class CityFixture
    {
        public const double START_GOLD = 1000;

        public CityFixture(params IBuildingDefinition[] extraBuildings)
        {
            Map = new FakeMap(-5, 5);
            Townhall = MakeTownhall();
            Buildings = new Catalog<IBuildingDefinition>(new[] { Townhall }.Concat(extraBuildings).Concat(ExtraBuildings()));
            Settings = new FakeSettings { Townhall = Townhall };
            Currencies = new Catalog<ICurrencyDefinition>(new ICurrencyDefinition[]
            {
                new FakeCurrency("Gold", CurrencyScope.City, START_GOLD),
                new FakeCurrency("Wood", CurrencyScope.City, 0),
                new FakeCurrency("Food", CurrencyScope.City, 0),
                new FakeCurrency("Mana", CurrencyScope.City, 100),
                new FakeCurrency("Knowledge", CurrencyScope.Kingdom, 0),
                new FakeCurrency("Gems", CurrencyScope.Player, 500),
            });
            Treasury = new Treasury(Currencies);

            (City, Ground) = NewCity.Create(Map, Settings);
            Placement = new Placement(Map, Buildings, Settings, City, Ground, Revealed);
            Construction = new Construction(City, Treasury, Placement, Buildings, Settings);
            Timeline = new Timeline(0);
            Timeline.Register(Construction);
        }

        // Buildings a derived fixture adds of its own.
        protected virtual IBuildingDefinition[] ExtraBuildings() => new IBuildingDefinition[0];

        protected virtual IBuildingDefinition MakeTownhall()
            => new BuildingBuilder().WithId("Townhall").WithMaxLevel(5).WithSize(2, 2).NotBuildable().Build();

        // Everything is the kingdom's ground unless a test fogs it.
        public FakeRevealed Revealed { get; } = new();

        public FakeMap Map { get; }
        public IBuildingDefinition Townhall { get; }
        public Catalog<IBuildingDefinition> Buildings { get; }
        public FakeSettings Settings { get; }
        public Catalog<ICurrencyDefinition> Currencies { get; }
        public Treasury Treasury { get; }
        public CityState City { get; }
        public GroundState Ground { get; }
        public Placement Placement { get; }
        public Construction Construction { get; }
        public Timeline Timeline { get; }

        public DistrictState District(string definitionId) => City.Districts.First(d => d.DefinitionId == definitionId);

        public sealed class FakeMap : IProvinceMap
        {
            private readonly Dictionary<Vector2Int, string> _terrain = new();
            private readonly Dictionary<Vector2Int, string> _features = new();

            public FakeMap(int from, int to)
            {
                for (var x = from; x <= to; x++)
                {
                    for (var y = from; y <= to; y++) _terrain[new Vector2Int(x, y)] = "Grassland";
                }
            }

            public IEnumerable<Vector2Int> Cells => _terrain.Keys;
            public bool Contains(Vector2Int cell) => _terrain.ContainsKey(cell);
            public string TerrainAt(Vector2Int cell) => _terrain.TryGetValue(cell, out var id) ? id : null;
            public string FeatureAt(Vector2Int cell) => _features.TryGetValue(cell, out var id) ? id : null;

            public void Paint(Vector2Int cell, string terrain) => _terrain[cell] = terrain;
            public void Place(Vector2Int cell, string feature) => _features[cell] = feature;
        }

        public sealed class FakeRevealed : IRevealedGround
        {
            public HashSet<Vector2Int> Fogged { get; } = new();
            public bool IsRevealed(Vector2Int cell) => !Fogged.Contains(cell);
        }

        public sealed class FakeSettings : IConstructionSettings
        {
            public IBuildingDefinition Townhall { get; set; }
            public int LateUpgradeFromLevel { get; set; } = 6;
            public int StartBuilders { get; set; } = 1;
            public int MaxBuilders { get; set; } = 4;
            public double BuilderGemCostBase { get; set; } = 2500;
            public double BuilderGemCostGrowth { get; set; } = 2;
        }

        private sealed class FakeCurrency : ICurrencyDefinition
        {
            public FakeCurrency(string id, CurrencyScope scope, double start)
            {
                Id = id;
                Scope = scope;
                Start = start;
            }

            public string Id { get; }
            public CurrencyScope Scope { get; }
            public double Start { get; }
            public double? Cap => null;
        }
    }
}
