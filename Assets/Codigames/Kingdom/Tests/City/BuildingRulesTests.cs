using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Tests.Builders;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.City
{
    public class BuildingRulesTests
    {
        private sealed class Settings : IConstructionSettings
        {
            public IBuildingDefinition Townhall { get; set; }
            public int LateUpgradeFromLevel => 6;
            public int StartBuilders => 1;
            public int MaxBuilders => 4;
            public double BuilderGemCostBase => 2500;
            public double BuilderGemCostGrowth => 2;
        }

        [Test]
        public void Problems_ShouldFindNoneInALegalBuilding()
        {
            Assert.That(BuildingRules.Problems(new BuildingBuilder().Build()), Is.Empty);
        }

        [Test]
        public void Problems_ShouldWantOnePricePerLevel()
        {
            var building = new BuildingBuilder().WithMaxLevel(3).WithLevelPrices(BuildingBuilder.Price("Gold", 10)).Build();

            Assert.That(BuildingRules.Problems(building), Has.Some.Contains("1 level prices for 3 levels"));
        }

        [Test]
        public void Problems_ShouldRefuseANegativePrice()
        {
            var building = new BuildingBuilder().WithMaxLevel(1).WithLevelPrices(BuildingBuilder.Price("Wood", -5)).Build();

            Assert.That(BuildingRules.Problems(building), Has.Some.Contains("negative amount of Wood"));
        }

        [Test]
        public void Problems_ShouldRefuseAnInstanceGrowthBelowOne()
        {
            var building = new BuildingBuilder().WithInstanceGrowth(2, 0.9).Build();

            Assert.That(BuildingRules.Problems(building), Has.Some.Contains("at least 1"));
        }

        [Test]
        public void Problems_ShouldRefuseMoreGatesThanLevelsToReach()
        {
            var building = new BuildingBuilder().WithMaxLevel(2).WithTownhallGates(1, 2).Build();

            Assert.That(BuildingRules.Problems(building), Has.Some.Contains("Townhall gates"));
        }

        [Test]
        public void Problems_ShouldBoundCountCapsByTheTownhall()
        {
            var townhall = new BuildingBuilder().WithId("Townhall").WithMaxLevel(2).Build();
            var house = new BuildingBuilder().WithId("Housing").WithMaxCountPerTownhallLevel(2, 4, 6).Build();

            var problems = BuildingRules.Problems(new[] { townhall, house }, new Settings { Townhall = townhall }).ToList();

            Assert.That(problems, Has.Count.EqualTo(1).And.Some.Contains("Housing"));
        }

        [Test]
        public void Problems_ShouldWantATownhall()
        {
            Assert.That(BuildingRules.Problems(new IBuildingDefinition[0], new Settings()), Has.Some.Contains("Townhall"));
        }
    }
}
