using System.Collections.Generic;
using Codigames.Kingdom.Tests.Builders;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Economy
{
    public class TreasuryTests
    {
        [Test]
        public void New_ShouldStartEveryCurrencyAtItsStart()
        {
            var treasury = new CityFixture().Treasury;

            Assert.That(treasury.Get("Gold"), Is.EqualTo(CityFixture.START_GOLD));
            Assert.That(treasury.Get("Gems"), Is.EqualTo(500));
            Assert.That(treasury.Get("Wood"), Is.EqualTo(0));
        }

        [Test]
        public void TryPay_ShouldPayAcrossHoldersOrNothing()
        {
            var treasury = new CityFixture().Treasury;
            treasury.Add("Knowledge", 5);

            Assert.That(treasury.TryPay(new Dictionary<string, double> { ["Gold"] = 100, ["Knowledge"] = 6 }), Is.False);
            Assert.That(treasury.Get("Gold"), Is.EqualTo(CityFixture.START_GOLD));

            Assert.That(treasury.TryPay(new Dictionary<string, double> { ["Gold"] = 100, ["Knowledge"] = 5 }), Is.True);
            Assert.That(treasury.Get("Gold"), Is.EqualTo(CityFixture.START_GOLD - 100));
            Assert.That(treasury.Get("Knowledge"), Is.EqualTo(0));
        }
    }
}
