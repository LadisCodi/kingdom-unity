using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.City
{
    public class AdjacencyTests
    {
        private sealed class Settings : IEconomySettings
        {
            public double GoldPerPopulationPerMinute => 30;
            public double CollectSeconds => 30;
        }

        private sealed class FakeRules : IAdjacencyRules
        {
            public IReadOnlyList<AdjacencyRule> Rules { get; set; } = new[]
            {
                new AdjacencyRule("Housing", "Housing", AdjacencyStat.GoldPerMinute, -1),
                new AdjacencyRule("Housing", "AnyDecoration", AdjacencyStat.GoldPerMinute, 1),
                new AdjacencyRule("Hall", "Hall", AdjacencyStat.TrainTime, -0.1),
            };
        }

        // 1×1 houses of 2, a 1×1 flower patch, 1×1 halls.
        private sealed class Fixture : CityFixture
        {
            public Fixture() : base(
                new BuildingBuilder().WithId("Housing").WithMaxLevel(1).WithBuildSeconds(10).WithHousing(2).WithStorage(100_000).Build(),
                new BuildingBuilder().WithId("Flowerbed").WithMaxLevel(1).WithBuildSeconds(10).WithHarmony(1).Build(),
                new BuildingBuilder().WithId("Hall").WithMaxLevel(1).WithBuildSeconds(10).Build())
            {
                Adjacency = new Adjacency(City, Buildings, new FakeRules(), new BuildingGroups(Buildings));
                Stores = new Stores(City, Buildings, new Settings(), Treasury, Construction, Bonuses, null, Harmony, Adjacency);
                Timeline.Register(Stores);
                City.Builders = 5;
            }

            public Adjacency Adjacency { get; }
            public Stores Stores { get; }
        }

        [Test]
        public void ShareEdge_ShouldCountAnEdge_AndNotACorner()
        {
            Assert.That(Adjacency.ShareEdge(new Vector2Int(0, 0), 1, 1, new Vector2Int(1, 0), 1, 1), Is.True);
            Assert.That(Adjacency.ShareEdge(new Vector2Int(0, 0), 2, 2, new Vector2Int(1, 2), 1, 1), Is.True);
            Assert.That(Adjacency.ShareEdge(new Vector2Int(0, 0), 1, 1, new Vector2Int(1, 1), 1, 1), Is.False);
            Assert.That(Adjacency.ShareEdge(new Vector2Int(0, 0), 1, 1, new Vector2Int(2, 0), 1, 1), Is.False);
        }

        [Test]
        public void HousesSideBySide_ShouldEachPayLess()
        {
            var fixture = new Fixture();
            var a = fixture.Stand("Housing", new Vector2Int(3, 3));
            var b = fixture.Stand("Housing", new Vector2Int(4, 3));
            fixture.City.Population = 4;

            Assert.That(fixture.Stores.GoldPerMinute(a), Is.EqualTo(59));
            Assert.That(fixture.Stores.GoldPerMinute(b), Is.EqualTo(59));
        }

        [Test]
        public void AHouseBesideADecoration_ShouldPayMore_AndAnEmptyOneNothing()
        {
            var fixture = new Fixture();
            var house = fixture.Stand("Housing", new Vector2Int(3, 3));
            fixture.Stand("Flowerbed", new Vector2Int(3, 4));

            Assert.That(fixture.Stores.GoldPerMinute(house), Is.EqualTo(0), "nobody lives there");

            fixture.City.Population = 2;
            Assert.That(fixture.Stores.GoldPerMinute(house), Is.EqualTo(61));
        }

        [Test]
        public void ATime_ShouldBeHeldWithinAQuarter()
        {
            var fixture = new Fixture();
            var hall = fixture.Stand("Hall", new Vector2Int(0, 3));
            foreach (var cell in new[] { new Vector2Int(-1, 3), new Vector2Int(1, 3), new Vector2Int(0, 2), new Vector2Int(0, 4) })
                fixture.Stand("Hall", cell);
            fixture.Stand("Hall", new Vector2Int(1, 4));

            Assert.That(fixture.Adjacency.Multiplier(hall, AdjacencyStat.TrainTime), Is.EqualTo(0.75).Within(1e-9));
        }

        [Test]
        public void Preview_ShouldSayWhatTheNeighboursGain_AndWhatItReceives()
        {
            var fixture = new Fixture();
            var house = fixture.Stand("Housing", new Vector2Int(3, 3));

            var preview = fixture.Adjacency.Preview("Housing", new Vector2Int(4, 3));

            Assert.That(preview.Given.Single().District, Is.SameAs(house));
            Assert.That(preview.Given.Single().Magnitude, Is.EqualTo(-1));
            Assert.That(preview.Received.Single().Total, Is.EqualTo(-1));
        }

        [Test]
        public void ANeighbourStanding_ShouldCountTheRentUntilThenAtTheOldRate()
        {
            var fixture = new Fixture();
            var house = fixture.Stand("Housing", new Vector2Int(3, 3));
            fixture.City.Population = 2;
            fixture.Stores.WakeAll(0);

            // 60 a minute for a minute, then the flower patch stands at 70 s: 61 a minute from there.
            fixture.Construction.Build("Flowerbed", new Vector2Int(3, 4), 60_000);
            fixture.Timeline.Advance(130_000);

            Assert.That(fixture.Stores.Held(house, 130_000), Is.EqualTo(60 + 10 + 61).Within(1));
        }

        [Test]
        public void OneAdvance_ShouldEqualSteppedTicking_WithANeighbourFinishing()
        {
            var once = new Fixture();
            var stepped = new Fixture();
            foreach (var f in new[] { once, stepped })
            {
                f.Stand("Housing", new Vector2Int(3, 3));
                f.City.Population = 2;
                f.Stores.WakeAll(0);
                f.Construction.Build("Flowerbed", new Vector2Int(3, 4), 1_000);
            }

            once.Timeline.Advance(3_600_000);
            for (var t = 0; t <= 3_600_000; t += 1_337) stepped.Timeline.Advance(t);
            stepped.Timeline.Advance(3_600_000);

            var a = once.City.Districts.First(d => d.DefinitionId == "Housing");
            var b = stepped.City.Districts.First(d => d.DefinitionId == "Housing");
            Assert.That(once.Stores.Held(a, 3_600_000), Is.EqualTo(stepped.Stores.Held(b, 3_600_000)));
        }
    }
}
