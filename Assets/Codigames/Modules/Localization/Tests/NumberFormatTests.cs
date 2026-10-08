using Codigames.Modules.Localization;
using NUnit.Framework;

namespace Codigames.Modules.Localization.Tests
{
    public class NumberFormatTests
    {
        private Localizer _localizer;
        private NumberFormat _format;

        [SetUp]
        public void SetUp()
        {
            _localizer = new Localizer();
            _format = new NumberFormat(_localizer);
        }

        [TestCase(9999, "9,999")]
        [TestCase(9999.5, "9,999.5")]
        [TestCase(12345, "12k")]
        [TestCase(248610, "248k")]
        [TestCase(1234567, "1.2M")]
        [TestCase(12345678, "12M")]
        public void Count_ShouldShortenPastTenThousand(double value, string expected)
        {
            Assert.That(_format.Count(value), Is.EqualTo(expected));
        }

        [TestCase(950, "950")]
        [TestCase(1290, "1.2k")]
        [TestCase(29999, "29k")]
        public void Short_ShouldFloorSoAStoreNeverReadsFull(double value, string expected)
        {
            Assert.That(_format.Short(value), Is.EqualTo(expected));
        }

        [Test]
        public void Number_ShouldFollowThePlayersLanguage()
        {
            _localizer.SetCulture("es-ES");

            Assert.That(_format.Exact(25000), Is.EqualTo("25.000"));
            Assert.That(_format.Usd(499), Is.EqualTo("$4,99"));
        }

        [TestCase(0, "instant")]
        [TestCase(45, "45s")]
        [TestCase(125, "2m 5s")]
        [TestCase(3600, "1h")]
        [TestCase(11400, "3h 10m")]
        [TestCase(100800, "1d 4h")]
        public void Duration_ShouldShowTheTwoLargestUnits(double seconds, string expected)
        {
            Assert.That(_format.Duration(seconds), Is.EqualTo(expected));
        }

        [Test]
        public void Countdown_ShouldRoundUpToWholeMinutesBelowAnHour()
        {
            Assert.That(_format.Countdown(125), Is.EqualTo("3m"));
            Assert.That(_format.Countdown(42), Is.EqualTo("42s"));
        }
    }
}
