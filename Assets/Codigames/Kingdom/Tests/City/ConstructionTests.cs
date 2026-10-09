using Codigames.Kingdom.City;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.City
{
    public class ConstructionTests
    {
        private static readonly Vector2Int SPOT = new(3, 3);

        private static IBuildingDefinition House(double gold = 100)
            => new BuildingBuilder().WithId("Housing").WithMaxLevel(3)
                .WithLevelPrices(BuildingBuilder.Price("Gold", gold), BuildingBuilder.Price("Gold", gold), BuildingBuilder.Price("Gold", gold))
                .WithBuildSeconds(60).WithUpgradeCurve(120, 1).WithTownhallGates(1, 2).Build();

        [Test]
        public void Build_ShouldPayAndPutABuilderToWork()
        {
            var fixture = new CityFixture(House());

            Assert.That(fixture.Construction.Build("Housing", SPOT, 1000), Is.EqualTo(ConstructionRefusal.None));
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(CityFixture.START_GOLD - 100));
            Assert.That(fixture.City.Jobs, Has.Count.EqualTo(1));
            Assert.That(fixture.District("Housing").Built, Is.False);
        }

        [Test]
        public void Build_ShouldFinishAtItsMoment()
        {
            var fixture = new CityFixture(House());
            fixture.Construction.Build("Housing", SPOT, 0);

            fixture.Timeline.Advance(59_999);
            Assert.That(fixture.District("Housing").Built, Is.False);

            fixture.Timeline.Advance(60_000);
            Assert.That(fixture.District("Housing").Built, Is.True);
            Assert.That(fixture.City.Jobs, Is.Empty);
        }

        [Test]
        public void Build_ShouldBeRefusedWithNoFreeBuilder()
        {
            var fixture = new CityFixture(House());
            fixture.Construction.Build("Housing", SPOT, 0);

            Assert.That(fixture.Construction.Build("Housing", new Vector2Int(-3, -3), 0), Is.EqualTo(ConstructionRefusal.NoFreeBuilder));
        }

        [Test]
        public void Build_ShouldBeRefusedWhenItCannotBePaid()
        {
            var fixture = new CityFixture(House(gold: CityFixture.START_GOLD + 100));

            Assert.That(fixture.Construction.Build("Housing", SPOT, 0), Is.EqualTo(ConstructionRefusal.CannotAfford));
            Assert.That(fixture.City.Districts, Has.Count.EqualTo(1));
        }

        [Test]
        public void Build_ShouldRefuseTheTownhall()
        {
            var fixture = new CityFixture(House());

            Assert.That(fixture.Construction.Build("Townhall", SPOT, 0), Is.EqualTo(ConstructionRefusal.NotBuildable));
        }

        [Test]
        public void Upgrade_ShouldRaiseTheLevelWhenTheJobEnds()
        {
            var fixture = new CityFixture(House());
            fixture.Construction.Build("Housing", SPOT, 0);
            fixture.Timeline.Advance(60_000);
            var house = fixture.District("Housing");

            Assert.That(fixture.Construction.Upgrade(house.Id, 60_000), Is.EqualTo(ConstructionRefusal.None));
            fixture.Timeline.Advance(180_000);

            Assert.That(house.Level, Is.EqualTo(2));
        }

        [Test]
        public void Upgrade_ShouldWaitForTheTownhallsLevel()
        {
            var fixture = new CityFixture(House());
            fixture.Construction.Build("Housing", SPOT, 0);
            fixture.Timeline.Advance(60_000);
            var house = fixture.District("Housing");
            house.Level = 2;

            Assert.That(fixture.Construction.Upgrade(house.Id, 60_000), Is.EqualTo(ConstructionRefusal.NeedsTownhallLevel));
        }

        [Test]
        public void Upgrade_ShouldStopAtTheMaxLevel()
        {
            var fixture = new CityFixture(House());
            var townhall = fixture.District("Townhall");
            townhall.Level = 5;

            Assert.That(fixture.Construction.Upgrade(townhall.Id, 0), Is.EqualTo(ConstructionRefusal.MaxLevel));
        }

        [Test]
        public void Move_ShouldBeFreeAndKeepTheJob()
        {
            var fixture = new CityFixture(House());
            fixture.Construction.Build("Housing", SPOT, 0);
            var house = fixture.District("Housing");
            var gold = fixture.Treasury.Get("Gold");

            Assert.That(fixture.Construction.Move(house.Id, new Vector2Int(-3, -3), 0), Is.EqualTo(ConstructionRefusal.None));
            Assert.That(house.Anchor, Is.EqualTo(new Vector2Int(-3, -3)));
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(gold));
            Assert.That(fixture.City.Jobs, Has.Count.EqualTo(1));
        }

        [Test]
        public void ALongAbsence_ShouldEndAsManyShortSteps()
        {
            var once = new CityFixture(House());
            var stepped = new CityFixture(House());
            once.Construction.Build("Housing", SPOT, 0);
            stepped.Construction.Build("Housing", SPOT, 0);

            once.Timeline.Advance(500_000);
            for (var t = 1_000; t <= 500_000; t += 1_000) stepped.Timeline.Advance(t);

            Assert.That(once.District("Housing").Built, Is.EqualTo(stepped.District("Housing").Built));
            Assert.That(once.City.Jobs.Count, Is.EqualTo(stepped.City.Jobs.Count));
        }
    

        [Test]
        public void BuildRefusal_ShouldNameTheCap()
        {
            var fixture = new CityFixture(new BuildingBuilder().WithId("Housing").WithMaxCountPerTownhallLevel(1)
                .WithLevelPrices(BuildingBuilder.Price("Gold", 10)).WithBuildSeconds(1).Build());
            fixture.Construction.Build("Housing", SPOT, 0);
            fixture.Timeline.Advance(1000);

            Assert.That(fixture.Construction.BuildRefusal("Housing"), Is.EqualTo(ConstructionRefusal.AtCap));
            Assert.That(fixture.Construction.Build("Housing", new Vector2Int(-3, -3), 1000), Is.EqualTo(ConstructionRefusal.AtCap));
        }

        [Test]
        public void Offer_ShouldPriceTheNextInstance()
        {
            var house = new BuildingBuilder().WithId("Housing").WithMaxLevel(3)
                .WithLevelPrices(BuildingBuilder.Price("Gold", 100)).WithInstanceGrowth(0.5, 1).WithBuildSeconds(60).Build();
            var fixture = new CityFixture(house);
            fixture.Construction.Build("Housing", SPOT, 0);

            var offer = fixture.Construction.Offer("Housing");

            Assert.That(offer.Ordinal, Is.EqualTo(2));
            Assert.That(offer.Count, Is.EqualTo(1));
            Assert.That(offer.Price["Gold"], Is.EqualTo(BuildingPricing.Currencies(house, 2, 1)["Gold"]));
            Assert.That(offer.Refusal, Is.EqualTo(ConstructionRefusal.NoFreeBuilder));
        }

        [Test]
        public void Offer_ShouldSayWhenItCannotBePaid()
        {
            var fixture = new CityFixture(House(gold: CityFixture.START_GOLD + 100));

            Assert.That(fixture.Construction.Offer("Housing").Refusal, Is.EqualTo(ConstructionRefusal.CannotAfford));
        }
    

        [Test]
        public void Offer_ShouldTimeTheWaitOnTheGivenPlot()
        {
            var house = new BuildingBuilder().WithId("Housing").WithLevelPrices(BuildingBuilder.Price("Gold", 10))
                .WithBuildSeconds(100).WithBuildGrowth(1, 2).Build();
            var fixture = new CityFixture(house);

            Assert.That(fixture.Construction.Offer("Housing").Seconds, Is.EqualTo(200));
            Assert.That(fixture.Construction.Offer("Housing", new Vector2Int(4, 0)).Seconds, Is.EqualTo(100 * 8));
        }
    

        [Test]
        public void UpgradeOffer_ShouldPriceAndTimeTheNextLevel()
        {
            var fixture = new CityFixture(House());
            fixture.Construction.Build("Housing", SPOT, 0);
            fixture.Timeline.Advance(60_000);
            var house = fixture.District("Housing");

            var offer = fixture.Construction.UpgradeOffer(house.Id);

            Assert.That(offer.TargetLevel, Is.EqualTo(2));
            Assert.That(offer.Price["Gold"], Is.EqualTo(100));
            Assert.That(offer.Seconds, Is.EqualTo(120));
            Assert.That(offer.Refusal, Is.EqualTo(ConstructionRefusal.None));
        }

        [Test]
        public void UpgradeOffer_ShouldNameTheTownhallItWaitsFor()
        {
            var fixture = new CityFixture(House());
            var house = BuiltHouseAtLevel(fixture, 2);

            var offer = fixture.Construction.UpgradeOffer(house.Id);

            Assert.That(offer.Refusal, Is.EqualTo(ConstructionRefusal.NeedsTownhallLevel));
            Assert.That(offer.RequiredTownhallLevel, Is.EqualTo(2));
        }

        [Test]
        public void UpgradeOffer_ShouldStopAtTheHighestLevel()
        {
            var fixture = new CityFixture(House());
            var house = BuiltHouseAtLevel(fixture, 3);

            Assert.That(fixture.Construction.UpgradeOffer(house.Id).Refusal, Is.EqualTo(ConstructionRefusal.MaxLevel));
        }

        private static Codigames.Kingdom.City.State.DistrictState BuiltHouseAtLevel(CityFixture fixture, int level)
        {
            fixture.Construction.Build("Housing", SPOT, 0);
            fixture.Timeline.Advance(60_000);
            var house = fixture.District("Housing");
            house.Level = level;
            return house;
        }
    

        [Test]
        public void Upgrade_ShouldWaitForTheVillagersItAsksFor()
        {
            var hall = new BuildingBuilder().WithId("Hall").WithMaxLevel(3).WithLevelPrices(BuildingBuilder.Price("Gold", 10),
                BuildingBuilder.Price("Gold", 10), BuildingBuilder.Price("Gold", 10)).WithBuildSeconds(1).WithPopulationGates(4).Build();
            var fixture = new CityFixture(hall);
            fixture.Construction.Build("Hall", SPOT, 0);
            fixture.Timeline.Advance(1000);
            var district = fixture.District("Hall");

            var offer = fixture.Construction.UpgradeOffer(district.Id);
            Assert.That(offer.Refusal, Is.EqualTo(ConstructionRefusal.NeedsPopulation));
            Assert.That(offer.RequiredPopulation, Is.EqualTo(4));

            fixture.City.Population = 4;
            Assert.That(fixture.Construction.Upgrade(district.Id, 1000), Is.EqualTo(ConstructionRefusal.None));
        }
    }
}
