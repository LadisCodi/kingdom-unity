using Codigames.Kingdom.City;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.City
{
    public class PlacementTests
    {
        private static IBuildingDefinition House(params int[] caps)
            => new BuildingBuilder().WithId("Housing").WithMaxCountPerTownhallLevel(caps).Build();

        [Test]
        public void Check_ShouldAcceptFreeDryGround()
        {
            var fixture = new CityFixture(House());

            Assert.That(fixture.Placement.Check("Housing", new Vector2Int(3, 3)), Is.EqualTo(PlacementProblem.None));
        }

        [Test]
        public void Check_ShouldRefuseOutsideTheProvince()
        {
            var fixture = new CityFixture(House());

            Assert.That(fixture.Placement.Check("Housing", new Vector2Int(6, 0)), Is.EqualTo(PlacementProblem.OutsideProvince));
        }

        [Test]
        public void Check_ShouldRefuseAFeatureOrABuilding()
        {
            var fixture = new CityFixture(House());
            fixture.Ground.Features[new Vector2Int(3, 3)] = "Trees";

            Assert.That(fixture.Placement.Check("Housing", new Vector2Int(3, 3)), Is.EqualTo(PlacementProblem.Occupied));
            Assert.That(fixture.Placement.Check("Housing", new Vector2Int(1, 1)), Is.EqualTo(PlacementProblem.Occupied));
        }

        [Test]
        public void Check_ShouldRefuseWater()
        {
            var fixture = new CityFixture(House());
            fixture.Map.Paint(new Vector2Int(3, 3), "Water");

            Assert.That(fixture.Placement.Check("Housing", new Vector2Int(3, 3)), Is.EqualTo(PlacementProblem.NeedsLand));
        }

        [Test]
        public void Check_ShouldRefusePastTheTownhallsCountCap()
        {
            var fixture = new CityFixture(House(1, 2));
            fixture.Construction.Build("Housing", new Vector2Int(3, 3), 0);

            Assert.That(fixture.Placement.Check("Housing", new Vector2Int(-3, -3)), Is.EqualTo(PlacementProblem.CountLimit));
        }

        [Test]
        public void Check_ShouldNotBlockAMoveWithItself()
        {
            var fixture = new CityFixture(House(1));
            fixture.Construction.Build("Housing", new Vector2Int(3, 3), 0);
            var house = fixture.District("Housing");

            Assert.That(fixture.Placement.Check("Housing", new Vector2Int(3, 3), house.Id), Is.EqualTo(PlacementProblem.None));
        }

        [Test]
        public void NewCity_ShouldClearTheFeaturesUnderTheTownhall()
        {
            var fixture = new CityFixture();

            Assert.That(fixture.Ground.Features.ContainsKey(new Vector2Int(0, 0)), Is.False);
            Assert.That(fixture.District("Townhall").Built, Is.True);
        }
    }
}
