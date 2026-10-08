using Codigames.Kingdom.Economy;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Economy
{
    public class PricesTests
    {
        [TestCase(1, 1)]
        [TestCase(12, 12)]
        [TestCase(123, 123)]
        [TestCase(1234, 1230)]
        [TestCase(12345, 12300)]
        [TestCase(123456, 123000)]
        [TestCase(1234567, 1230000)]
        [TestCase(0, 0)]
        [TestCase(0.4, 0)]
        [TestCase(12.6, 13)]
        [TestCase(999.4, 999)]
        [TestCase(999.6, 1000)]
        [TestCase(15847, 15800)]
        [TestCase(99960, 100000)]
        [TestCase(12.5, 13)]
        [TestCase(-12.5, -13)]
        public void RoundPrice_ShouldKeepThreeSignificantFigures(double n, double expected)
        {
            Assert.That(Prices.RoundPrice(n), Is.EqualTo(expected));
        }
    }
}
