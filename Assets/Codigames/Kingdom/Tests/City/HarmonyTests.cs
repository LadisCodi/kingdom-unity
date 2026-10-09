using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.City
{
    public class HarmonyTests
    {
        private sealed class Settings : IEconomySettings
        {
            public double GoldPerPopulationPerMinute => 30;
            public double CollectSeconds => 30;
        }

        // Houses that demand 0, 2, then 4 Harmony; a Garden that supplies 4 and a flower patch that supplies 1.
        private sealed class Fixture : CityFixture
        {
            public Fixture() : base(House(), Decoration("Garden", 4), Decoration("Flowerbed", 1))
            {
                Stores = new Stores(City, Buildings, new Settings(), Treasury, Construction, Bonuses, null, Harmony);
                Timeline.Register(Stores);
                City.Builders = 3;
            }

            public Stores Stores { get; }

            private static IBuildingDefinition House()
                => new BuildingBuilder().WithId("Housing").WithMaxLevel(3).WithBuildSeconds(10).WithHousing(2, 2, 2)
                    .WithStorage(3600, 3600, 3600).WithHarmonyCost(0, 2, 4).Build();

            private static IBuildingDefinition Decoration(string id, double supply)
                => new BuildingBuilder().WithId(id).WithMaxLevel(1).WithBuildSeconds(10).WithHarmony(supply).Build();
        }

        [Test]
        public void Supply_ShouldCountADecorationOnceItStands_AndNotBefore()
        {
            var fixture = new Fixture();

            fixture.Construction.Build("Garden", new Vector2Int(3, 3), 0);
            Assert.That(fixture.Harmony.Supply, Is.EqualTo(0));

            fixture.Timeline.Advance(10_000);
            Assert.That(fixture.Harmony.Supply, Is.EqualTo(4));
        }

        [Test]
        public void Demand_ShouldBeATotal_AndALevelGoingUpCountsAtItsTarget()
        {
            var fixture = new Fixture();
            var house = fixture.Stand("Housing", new Vector2Int(3, 3), 2);

            Assert.That(fixture.Harmony.Demand(), Is.EqualTo(2));

            fixture.City.Jobs.Add(new ConstructionJob { Id = "job-x", DistrictId = house.Id, TargetLevel = 3, StartedAt = 0, Seconds = 60 });
            Assert.That(fixture.Harmony.Demand(), Is.EqualTo(4));
        }

        [Test]
        public void Upgrade_ShouldBeRefused_WhenItWouldTakeTheCityPastItsSupply()
        {
            var fixture = new Fixture();
            var house = fixture.Stand("Housing", new Vector2Int(3, 3));

            Assert.That(fixture.Construction.UpgradeRefusal(house), Is.EqualTo(ConstructionRefusal.NeedsHarmony));

            fixture.Stand("Flowerbed", new Vector2Int(-3, -3));
            fixture.Stand("Flowerbed", new Vector2Int(-3, 3));
            Assert.That(fixture.Construction.UpgradeRefusal(house), Is.EqualTo(ConstructionRefusal.None));
        }

        [Test]
        public void ALevel_ShouldReplaceItsBuildingsDemand_NotStackOnIt()
        {
            var fixture = new Fixture();
            var house = fixture.Stand("Housing", new Vector2Int(3, 3), 2);
            fixture.Stand("Flowerbed", new Vector2Int(-3, -3));
            fixture.Stand("Flowerbed", new Vector2Int(-3, 3));
            fixture.Stand("Flowerbed", new Vector2Int(3, -3));

            // 4 for level 3 in place of the 2 it asks now, against 3 supplied.
            Assert.That(fixture.Harmony.ShortBy(fixture.Buildings.Get("Housing"), 3, house.Id), Is.EqualTo(1));
        }

        [Test]
        public void Tier_ShouldBeTheLastTheRatioReaches_AndNoneWhenNothingIsDemanded()
        {
            var fixture = new Fixture();
            fixture.Stand("Garden", new Vector2Int(-3, -3));
            Assert.That(fixture.Harmony.Tier, Is.Null);
            Assert.That(fixture.Harmony.Multiplier, Is.EqualTo(1));

            // 4 supplied of 4 demanded: no tier yet.
            fixture.Stand("Housing", new Vector2Int(3, 3), 3);
            Assert.That(fixture.Harmony.Tier, Is.Null);

            // 5 of 4 is 125%: +10%.
            fixture.Stand("Flowerbed", new Vector2Int(-3, 3));
            Assert.That(fixture.Harmony.Multiplier, Is.EqualTo(1.1).Within(1e-9));

            // 6 of 4 is 150%: +15%.
            fixture.Stand("Flowerbed", new Vector2Int(3, -3));
            Assert.That(fixture.Harmony.Multiplier, Is.EqualTo(1.15).Within(1e-9));
        }

        [Test]
        public void ASurplus_ShouldRaiseTheRentAtItsBase()
        {
            var fixture = new Fixture();
            var house = fixture.Stand("Housing", new Vector2Int(3, 3), 3);
            fixture.City.Population = 2;
            Assert.That(fixture.Stores.GoldPerMinute(house), Is.EqualTo(60));

            fixture.Stand("Garden", new Vector2Int(-3, -3));
            fixture.Stand("Flowerbed", new Vector2Int(-3, 3));
            fixture.Stand("Flowerbed", new Vector2Int(3, -3));

            Assert.That(fixture.Stores.GoldPerMinute(house), Is.EqualTo(69).Within(1e-9));
        }
    
        [Test]
        public void UpgradeRequirements_ShouldListHarmony_OnlyWhenTheLevelAsksMore()
        {
            var fixture = new Fixture();
            var house = fixture.Stand("Housing", new Vector2Int(3, 3));

            var gate = fixture.Construction.UpgradeRequirements(house.Id);
            Assert.That(gate, Has.Some.Matches<UpgradeRequirement>(r => r.Kind == RequirementKind.Harmony && r.Amount == 2 && !r.Met));

            house.Level = 3;
            Assert.That(fixture.Construction.UpgradeRequirements(house.Id), Is.Empty, "at the top");
        }
    }
}
