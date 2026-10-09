using System.Collections.Generic;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Fog.State;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Fog
{
    public class TreasuresTests
    {
        private sealed class FogSettings : IFogSettings
        {
            public IReadOnlyList<double> CostPerRing => new double[] { 4, 8, 20, 85 };
            public double FallbackGrowth => 2;
            public int TapsToReveal => 5;
            public double MinCost => 1;
            public int CountStep => 10;
            public double CountGrowth => 1.05;
            public IReadOnlyList<int> ReachPerTownhallLevel => new[] { 6 };
        }

        private sealed class Settings : ITreasureSettings
        {
            public int EveryReveals => 5;
            public double WorkSeconds => 120;
            public IReadOnlyDictionary<string, double> Floor => new Dictionary<string, double> { ["Gold"] = 10, ["Wood"] = 5, ["Food"] = 5 };
            public IReadOnlyDictionary<string, double> Weights => new Dictionary<string, double> { ["Gold"] = 3, ["Wood"] = 3, ["Food"] = 3, ["Knowledge"] = 1 };
            public double Knowledge => 1;
            public string FirstCoin => "Gold";
            public double FirstAmount => 20;
        }

        private sealed class Made : IProduction
        {
            public double PerSecond { get; set; }
            public double MakesPerSecond(string currency) => PerSecond;
        }

        private sealed class NoSites : ISiteGround
        {
            public bool Holds(Vector2Int cell) => false;
        }

        // The Townhall's ground revealed, its ring of two seen; the reach six rings out.
        private sealed class Fixture : CityFixture
        {
            public Fixture(uint seed = 7)
                : base(new BuildingBuilder().WithId("Housing").WithLevelPrices(BuildingBuilder.Price("Gold", 10)).WithBuildSeconds(1).Build())
            {
                Fog = new FogOfWar(new FogState(), City, Map, Buildings, Settings, new FogSettings(), Treasury);
                Treasures = new Treasures(State, Fog, Map, null, Ground, new NoSites(), new Settings(), Treasury, Production, Gates,
                    new Catalog<IHarvestSource>(new IHarvestSource[0]), Buildings, Construction, seed);
            }

            public FogOfWar Fog { get; }
            public FogState State => (FogState)typeof(FogOfWar).GetField("_state", System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance).GetValue(Fog);
            public Made Production { get; } = new();
            public Treasures Treasures { get; }

            public void Clear(Vector2Int cell)
            {
                for (var i = 0; i < 5; i++) Fog.Tap(cell);
            }

            protected override IBuildingDefinition MakeTownhall()
                => new BuildingBuilder().WithId("Townhall").WithMaxLevel(5).WithSize(2, 2).NotBuildable().WithFog(1, 2).Build();
        }

        [Test]
        public void TheFirstPaidReveal_ShouldSetDownTheFirstTreasureInSight()
        {
            var fixture = new Fixture();

            fixture.Clear(new Vector2Int(3, 0));

            var placed = fixture.Treasures.At(new Vector2Int(4, 0));
            Assert.That(placed, Is.Not.Null, "the one cell the reveal brought into view");
            Assert.That(placed.N, Is.EqualTo(0));
            Assert.That(fixture.Treasures.Reward(placed), Is.EquivalentTo(new Dictionary<string, double> { ["Gold"] = 20 }));
        }

        [Test]
        public void ATreasure_ShouldBeDueEveryFiveCellsPaidFor()
        {
            var fixture = new Fixture();
            var cells = new[] { new Vector2Int(3, 0), new Vector2Int(3, 1), new Vector2Int(3, 2), new Vector2Int(3, -1), new Vector2Int(-2, 0) };

            foreach (var cell in cells) fixture.Clear(cell);
            Assert.That(fixture.State.TreasuresPlaced, Is.EqualTo(1));

            fixture.Clear(new Vector2Int(-2, 1));
            Assert.That(fixture.State.TreasuresPlaced, Is.EqualTo(2));
        }

        [Test]
        public void PickUp_ShouldWaitForTheCellToBeRevealedAndPayOnce()
        {
            var fixture = new Fixture();
            fixture.Clear(new Vector2Int(3, 0));
            var cell = new Vector2Int(4, 0);
            var gold = fixture.Treasury.Get("Gold");

            Assert.That(fixture.Treasures.PickUp(cell), Is.Null, "still under the fog");

            fixture.Clear(cell);
            gold = fixture.Treasury.Get("Gold");
            Assert.That(fixture.Treasures.PickUp(cell)?["Gold"], Is.EqualTo(20));
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(gold + 20));
            Assert.That(fixture.Treasures.PickUp(cell), Is.Null);
        }

        [Test]
        public void ALaterTreasure_ShouldPaySecondsOfWhatTheCityMakesFlooredAndRounded()
        {
            var fixture = new Fixture();
            var treasure = new Treasure { N = 3, Coin = "Wood" };

            Assert.That(fixture.Treasures.Reward(treasure)["Wood"], Is.EqualTo(5), "the floor");
            fixture.Production.PerSecond = 10.37;
            Assert.That(fixture.Treasures.Reward(treasure)["Wood"], Is.EqualTo(1240), "1,244.4 to three figures");
            Assert.That(fixture.Treasures.Reward(new Treasure { N = 4, Coin = "Knowledge" })["Knowledge"], Is.EqualTo(1));
        }

        [Test]
        public void WhereAndWhat_ShouldDependOnlyOnTheSeedAndTheTreasuresNumber()
        {
            Vector2Int Place(uint seed)
            {
                var fixture = new Fixture(seed);
                foreach (var cell in new[] { new Vector2Int(3, 0), new Vector2Int(3, 1), new Vector2Int(3, 2), new Vector2Int(3, -1), new Vector2Int(-2, 0), new Vector2Int(-2, 1) })
                    fixture.Clear(cell);
                foreach (var placed in fixture.Treasures.All) if (placed.Value.N == 1) return placed.Key;
                return new Vector2Int(99, 99);
            }

            Assert.That(Place(11), Is.EqualTo(Place(11)));
        }

        [Test]
        public void Building_ShouldPickUpATreasureRatherThanBuryIt()
        {
            var fixture = new Fixture();
            fixture.Clear(new Vector2Int(3, 0));
            fixture.Clear(new Vector2Int(4, 0));
            var gold = fixture.Treasury.Get("Gold");

            Assert.That(fixture.Construction.Build("Housing", new Vector2Int(4, 0), 0), Is.EqualTo(ConstructionRefusal.None));

            Assert.That(fixture.Treasures.At(new Vector2Int(4, 0)), Is.Null);
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(gold - 10 + 20));
        }
    }
}
