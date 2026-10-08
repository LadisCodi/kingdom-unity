using Codigames.Kingdom.City;
using Codigames.Kingdom.Tests.Builders;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.City
{
    public class BuildingDurationsTests
    {
        [Test]
        public void BuildSeconds_ShouldGrowWithCountAndDistance()
        {
            var building = new BuildingBuilder().WithBuildSeconds(100).WithBuildGrowth(1.5, 1.1).Build();

            Assert.That(BuildingDurations.BuildSeconds(building.Duration, 0, 0), Is.EqualTo(100));
            Assert.That(BuildingDurations.BuildSeconds(building.Duration, 1, 0), Is.EqualTo(150));
            Assert.That(BuildingDurations.BuildSeconds(building.Duration, 0, 2), Is.EqualTo(121));
        }

        [Test]
        public void UpgradeSeconds_ShouldPivotToTheLateCurve()
        {
            var building = new BuildingBuilder().WithUpgradeCurve(60, 2, 7200, 1.5).Build();

            Assert.That(BuildingDurations.UpgradeSeconds(building.Duration, 2, 6), Is.EqualTo(60));
            Assert.That(BuildingDurations.UpgradeSeconds(building.Duration, 5, 6), Is.EqualTo(480));
            Assert.That(BuildingDurations.UpgradeSeconds(building.Duration, 6, 6), Is.EqualTo(7200));
            Assert.That(BuildingDurations.UpgradeSeconds(building.Duration, 7, 6), Is.EqualTo(10800));
        }

        [Test]
        public void UpgradeSeconds_ShouldContinueTheEarlyCurveWithNoLateLevels()
        {
            var building = new BuildingBuilder().WithUpgradeCurve(60, 2).Build();

            Assert.That(BuildingDurations.UpgradeSeconds(building.Duration, 7, 6), Is.EqualTo(1920));
        }
    }
}
