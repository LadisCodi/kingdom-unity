using Codigames.Kingdom.City;
using Codigames.Kingdom.Tests.Builders;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.City
{
    public class BuildingPricingTests
    {
        [TestCase(1, 1.0)]
        [TestCase(2, 3.2)]
        [TestCase(3, 5.44)]
        [TestCase(5, 10.0736)]
        public void InstanceMultiplier_ShouldFollowTheTwoTerms(int ordinal, double expected)
        {
            var building = new BuildingBuilder().WithInstanceGrowth(2, 1.2).Build();

            Assert.That(BuildingPricing.InstanceMultiplier(building.Cost, ordinal), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void Currencies_ShouldPriceTheLevelForTheInstanceInThreeFigures()
        {
            var building = new BuildingBuilder().WithMaxLevel(2)
                .WithLevelPrices(BuildingBuilder.Price("Gold", 15), BuildingBuilder.Price("Gold", 1234))
                .WithInstanceGrowth(2, 1.2).Build();

            Assert.That(BuildingPricing.Currencies(building, 1, 1)["Gold"], Is.EqualTo(15));
            Assert.That(BuildingPricing.Currencies(building, 1, 2)["Gold"], Is.EqualTo(1230));
            // 1,234 × 3.2 = 3,948.8.
            Assert.That(BuildingPricing.Currencies(building, 2, 2)["Gold"], Is.EqualTo(3950));
        }
    }
}
