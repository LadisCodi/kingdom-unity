using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.City
{
    public class BuildingStatsTests
    {
        private static readonly Vector2Int SPOT = new(3, 3);

        private static IBuildingDefinition House()
            => new BuildingBuilder().WithId("Housing").WithMaxLevel(3)
                .WithLevelPrices(BuildingBuilder.Price("Gold", 10), BuildingBuilder.Price("Gold", 10), BuildingBuilder.Price("Gold", 10))
                .WithBuildSeconds(60).WithUpgradeCurve(120, 1).WithTownhallGates(1, 2).WithPopulationGates(0, 4)
                .WithHousing(2, 4, 6).WithTaxBonus(0, 0.25, 0.5).Build();

        private static IBuildingDefinition Sawmill()
            => new BuildingBuilder().WithId("Sawmill").WithMaxLevel(2)
                .WithLevelPrices(BuildingBuilder.Price("Gold", 10), BuildingBuilder.Price("Gold", 10))
                .WithBuildSeconds(60).WithUpgradeCurve(120, 1).WithStorage(100, 250).WithCrew("Forest", 3, 2).Build();

        private static DistrictState Built(CityFixture fixture, string id, int level = 1)
        {
            fixture.Construction.Build(id, SPOT, 0);
            fixture.Timeline.Advance(60_000);
            var district = fixture.District(id);
            district.Level = level;
            return district;
        }

        private static BuildingStats Stats(CityFixture fixture) => new(fixture.Buildings, fixture.Settings, fixture.Bonuses);

        [Test]
        public void At_ShouldReadEachFigureAtTheLevelAsked()
        {
            var fixture = new CityFixture(House());
            var house = Built(fixture, "Housing");

            var level2 = Stats(fixture).At(house, 2);

            Assert.That(level2.Single(s => s.Kind == StatKind.Beds).Value, Is.EqualTo(4));
            Assert.That(level2.Single(s => s.Kind == StatKind.Rent).Value, Is.EqualTo(25));
            Assert.That(level2.Single(s => s.Kind == StatKind.Rent).OnCard, Is.False, "the card's Gold an hour counts the rent in");
        }

        [Test]
        public void Changes_ShouldListOnlyWhatTheNextLevelMoves()
        {
            var fixture = new CityFixture(Sawmill());
            var sawmill = Built(fixture, "Sawmill");

            var changes = Stats(fixture).Changes(sawmill);

            Assert.That(changes.Select(c => c.Now.Kind), Is.EqualTo(new[] { StatKind.Storage }));
            Assert.That(changes[0].Now.Value, Is.EqualTo(100));
            Assert.That(changes[0].Delta, Is.EqualTo(150));
            Assert.That(changes[0].Better, Is.True);
        }

        [Test]
        public void TheTownhall_ShouldCarryItsOwnFigures()
        {
            var fixture = new CityFixture();
            var townhall = fixture.District(fixture.Townhall.Id);

            var kinds = Stats(fixture).At(townhall, 1).Select(s => s.Kind);

            Assert.That(kinds, Has.No.Member(StatKind.Beds));
        }

        [Test]
        public void UpgradeRequirements_ShouldListEveryGateMetOrNot()
        {
            var tech = new TechBuilder("Masonry").Opens(UnlockKind.DistrictLevel, "Housing", 3).Build();
            var fixture = new CityFixture(new[] { tech }, House());
            var house = Built(fixture, "Housing", 2);

            var requirements = fixture.Construction.UpgradeRequirements(house.Id);

            Assert.That(requirements.Select(r => r.Kind),
                Is.EqualTo(new[] { RequirementKind.TownhallLevel, RequirementKind.Research, RequirementKind.Population }));
            Assert.That(requirements.Select(r => r.Met), Is.EqualTo(new[] { false, false, false }));
            Assert.That(requirements[0].Amount, Is.EqualTo(2));
            Assert.That(requirements[1].Tech, Is.EqualTo("Masonry"));
            Assert.That(requirements[2].Amount, Is.EqualTo(4));

            fixture.ResearchState.Completed.Add("Masonry");
            Assert.That(fixture.Construction.UpgradeRequirements(house.Id)[1].Met, Is.True);
        }

        [Test]
        public void UpgradeRequirements_ShouldBeEmptyAtTheTop()
        {
            var fixture = new CityFixture(House());
            var house = Built(fixture, "Housing", 3);

            Assert.That(fixture.Construction.UpgradeRequirements(house.Id), Is.Empty);
        }
    }
}
