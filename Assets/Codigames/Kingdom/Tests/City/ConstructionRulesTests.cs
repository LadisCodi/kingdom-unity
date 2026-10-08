using Codigames.Kingdom.City;
using Codigames.Kingdom.Tests.Builders;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.City
{
    public class ConstructionRulesTests
    {
        private sealed class Settings : IConstructionSettings
        {
            public IBuildingDefinition Townhall { get; set; } = new BuildingBuilder().WithId("Townhall").Build();
            public int LateUpgradeFromLevel { get; set; } = 6;
            public int StartBuilders { get; set; } = 1;
            public int MaxBuilders { get; set; } = 4;
            public double BuilderGemCostBase { get; set; } = 2500;
            public double BuilderGemCostGrowth { get; set; } = 2;
        }

        [Test]
        public void Problems_ShouldFindNoneInLegalSettings()
        {
            Assert.That(ConstructionRules.Problems(new Settings()), Is.Empty);
        }

        [Test]
        public void Problems_ShouldRefuseACeilingBelowTheStart()
        {
            Assert.That(ConstructionRules.Problems(new Settings { StartBuilders = 3, MaxBuilders = 2 }), Has.Some.Contains("ceiling"));
        }

        [Test]
        public void Problems_ShouldWantATownhall()
        {
            Assert.That(ConstructionRules.Problems(new Settings { Townhall = null }), Has.Some.Contains("Townhall"));
        }
    }
}
