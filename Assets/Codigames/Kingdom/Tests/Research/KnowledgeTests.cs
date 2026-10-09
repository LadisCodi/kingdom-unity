using Codigames.Kingdom.Research;
using Codigames.Kingdom.Tests.Builders;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Research
{
    public class KnowledgeTests
    {
        private const double HOUR = 3_600_000;

        [Test]
        public void Drip_ShouldPayAPointAnHourUpToTheCap()
        {
            var fixture = new ResearchFixture();
            fixture.Bar.Wake(0);

            fixture.Timeline.Advance(3.5 * HOUR);
            Assert.That(fixture.Bar.Amount, Is.EqualTo(3));

            fixture.Timeline.Advance(30 * HOUR);
            Assert.That(fixture.Bar.Amount, Is.EqualTo(10));
            Assert.That(fixture.Bar.NextUnitAt, Is.Null);
        }

        [Test]
        public void Drip_ShouldReplayAnAbsenceInOneCallAsItTicks()
        {
            var once = new ResearchFixture();
            var stepped = new ResearchFixture();
            once.Bar.Wake(0);
            stepped.Bar.Wake(0);

            once.Timeline.Advance(7.25 * HOUR);
            for (var t = 0.0; t <= 7.25 * HOUR; t += 0.25 * HOUR) stepped.Timeline.Advance(t);

            Assert.That(once.Bar.Amount, Is.EqualTo(stepped.Bar.Amount));
            Assert.That(once.Knowledge.AccruingSince, Is.EqualTo(stepped.Knowledge.AccruingSince));
        }

        [Test]
        public void ALump_ShouldLandOverTheCapAndOnlyTheDripStops()
        {
            var fixture = new ResearchFixture();
            fixture.GiveKnowledge(9);
            fixture.Bar.Wake(0);
            fixture.GiveKnowledge(5);

            fixture.Timeline.Advance(5 * HOUR);

            Assert.That(fixture.Bar.Amount, Is.EqualTo(14));
        }

        [Test]
        public void FullAt_ShouldCountTheMissingPointsFromTheNextOne()
        {
            var fixture = new ResearchFixture();
            fixture.GiveKnowledge(7);
            fixture.Bar.Wake(0);

            Assert.That(fixture.Bar.FullAt(0), Is.EqualTo(3 * HOUR));
        }

        [Test]
        public void GoldPrice_ShouldRiseWithEveryPointEverBought()
        {
            var fixture = new ResearchFixture();

            Assert.That(fixture.Market.GoldPrice(1), Is.EqualTo(400));
            Assert.That(fixture.Market.GoldPrice(2), Is.EqualTo(2000));

            Assert.That(fixture.Market.Buy(1, KnowledgeMarket.GOLD), Is.EqualTo(BuyResult.Bought));
            Assert.That(fixture.Market.GoldPrice(1), Is.EqualTo(1600));
            Assert.That(fixture.Bar.Amount, Is.EqualTo(1));
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(CityFixture.START_GOLD - 400));
        }

        [Test]
        public void GemPrice_ShouldNeverRise()
        {
            var fixture = new ResearchFixture();

            Assert.That(fixture.Market.Buy(2, KnowledgeMarket.GEMS), Is.EqualTo(BuyResult.Bought));
            Assert.That(fixture.Market.GemPrice(1), Is.EqualTo(200));
            Assert.That(fixture.Treasury.Get("Gems"), Is.EqualTo(100));
            Assert.That(fixture.Market.Buy(1, KnowledgeMarket.GEMS), Is.EqualTo(BuyResult.CannotAfford));
        }
    }
}
