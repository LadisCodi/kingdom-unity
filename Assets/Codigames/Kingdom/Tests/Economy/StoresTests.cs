using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Economy
{
    public class StoresTests
    {
        private sealed class Settings : IEconomySettings
        {
            public double GoldPerPopulationPerMinute => 30;
            public double CollectSeconds => 30;
        }

        // A Townhall that makes 10 Gold a minute into a store of 600, and houses of 2 that keep 3,600.
        private sealed class Fixture : CityFixture
        {
            public Fixture() : base(House())
            {
                Stores = new Stores(City, Buildings, new Settings(), Treasury, Construction);
                Timeline.Register(Stores);
                Stores.WakeAll(0);
            }

            public Stores Stores { get; }

            public DistrictState Hall => City.Districts[0];

            protected override IBuildingDefinition MakeTownhall()
                => new BuildingBuilder().WithId("Townhall").WithMaxLevel(5).WithSize(2, 2).NotBuildable()
                    .WithOwnGold(10, 60).WithStorage(600, 4700).Build();

            private static IBuildingDefinition House()
                => new BuildingBuilder().WithId("Housing").WithMaxLevel(3).WithLevelPrices(BuildingBuilder.Price("Gold", 10),
                        BuildingBuilder.Price("Gold", 10), BuildingBuilder.Price("Gold", 10))
                    .WithBuildSeconds(10).WithHousing(2, 4).WithTaxBonus(0, 0.25).WithStorage(3600, 12000).Build();
        }

        [Test]
        public void Townhall_ShouldFillItsStoreWithItsOwnGold()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Stores.Held(fixture.Hall, 59_999), Is.EqualTo(9));
            Assert.That(fixture.Stores.Held(fixture.Hall, 60_000), Is.EqualTo(10));
        }

        [Test]
        public void Store_ShouldBeReadyAfterHalfAMinuteOfItsMaking()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Stores.IsReady(fixture.Hall, 29_000), Is.False);
            Assert.That(fixture.Stores.IsReady(fixture.Hall, 30_000), Is.True);
        }

        [Test]
        public void Collect_ShouldMoveTheWholeStoreToTheTreasury()
        {
            var fixture = new Fixture();
            var gold = fixture.Treasury.Get("Gold");

            var moved = fixture.Stores.Collect(fixture.Hall, 90_000);

            Assert.That(moved["Gold"], Is.EqualTo(15));
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(gold + 15));
            Assert.That(fixture.Stores.Held(fixture.Hall, 90_000), Is.EqualTo(0));
            Assert.That(fixture.Stores.Held(fixture.Hall, 150_000), Is.EqualTo(10));
        }

        [Test]
        public void FullStore_ShouldStopItsBuildingUntilCollected()
        {
            var fixture = new Fixture();

            fixture.Timeline.Advance(10 * 3_600_000);

            Assert.That(fixture.Stores.Held(fixture.Hall, 10 * 3_600_000), Is.EqualTo(600));
            Assert.That(fixture.Hall.Store.AccruingSince, Is.Null);

            fixture.Stores.Collect(fixture.Hall, 10 * 3_600_000);
            Assert.That(fixture.Stores.Held(fixture.Hall, 10 * 3_600_000 + 60_000), Is.EqualTo(10), "starts again from the collect");
        }

        [Test]
        public void Houses_ShouldFillInBuildOrderAndPayRent()
        {
            var fixture = new Fixture();
            fixture.Construction.Build("Housing", new Vector2Int(3, 3), 0);
            fixture.Timeline.Advance(10_000);
            fixture.City.Builders = 2;
            fixture.Construction.Build("Housing", new Vector2Int(-3, -3), 10_000);
            fixture.Timeline.Advance(20_000);
            var first = fixture.City.Districts[1];
            var second = fixture.City.Districts[2];

            fixture.City.Population = 3;

            Assert.That(fixture.Stores.Residents(first), Is.EqualTo(2));
            Assert.That(fixture.Stores.Residents(second), Is.EqualTo(1));
            Assert.That(fixture.Stores.GoldPerMinute(first), Is.EqualTo(60));
            Assert.That(fixture.Stores.Housing, Is.EqualTo(4));
        }

        [Test]
        public void ALevel_ShouldBeCountedAtTheRateItHadUntilItFinished()
        {
            var fixture = new Fixture();
            fixture.Hall.Level = 1;
            fixture.City.Jobs.Add(new ConstructionJob { Id = "job-x", DistrictId = fixture.Hall.Id, TargetLevel = 2, StartedAt = 0, Seconds = 60 });

            fixture.Timeline.Advance(120_000);

            // A minute at 10, then a minute at 60.
            Assert.That(fixture.Stores.Held(fixture.Hall, 120_000), Is.EqualTo(70));
        }

        [Test]
        public void OneAdvance_ShouldEqualSteppedTicking()
        {
            var once = new Fixture();
            var stepped = new Fixture();
            foreach (var f in new[] { once, stepped })
                f.City.Jobs.Add(new ConstructionJob { Id = "job-x", DistrictId = f.Hall.Id, TargetLevel = 2, StartedAt = 0, Seconds = 77 });

            once.Timeline.Advance(7_200_000);
            for (var t = 0; t <= 7_200_000; t += 1_337) stepped.Timeline.Advance(t);
            stepped.Timeline.Advance(7_200_000);

            Assert.That(once.Stores.Held(once.Hall, 7_200_000), Is.EqualTo(stepped.Stores.Held(stepped.Hall, 7_200_000)));
            Assert.That(once.Hall.Store.AccruingSince, Is.EqualTo(stepped.Hall.Store.AccruingSince));
        }
    }
}
