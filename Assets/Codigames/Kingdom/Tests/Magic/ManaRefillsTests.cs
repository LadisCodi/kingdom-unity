using System;
using System.Collections.Generic;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Magic.State;
using Codigames.Kingdom.Tests.Builders;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Magic
{
    public class ManaRefillsTests
    {
        private sealed class Mana : IManaSettings
        {
            public double BaseCap => 100;
            public double BasePerHour => 12;
            public double LandmarkCap => 10;
        }

        private sealed class Settings : IRefillSettings
        {
            public double CooldownMinSeconds => 600;
            public double CooldownMaxSeconds => 1200;
            public double EligibleBelowFraction => 0.5;
            public int RefillsPerDay => 2;
            public IReadOnlyList<double> GemRefillCosts { get; } = new double[] { 100, 200 };
        }

        private const double HOUR = 3_600_000;
        private static readonly double T0 = new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

        private CityFixture _city;
        private ManaPool _pool;
        private AdsState _state;
        private ManaRefills _refills;

        [SetUp]
        public void SetUp()
        {
            _city = new CityFixture();
            _pool = new ManaPool(new ManaState(), _city.Treasury, new Mana());
            _state = new AdsState();
            _refills = new ManaRefills(_state, _pool, new Settings(), _city.Treasury, 7);
            _city.Treasury.Add("Gems", 10_000);
        }

        private void Spend(double mana) => _pool.TrySpend(mana, T0);

        [Test]
        public void AGemRefill_ShouldLandAWholePoolOnTop_AndClimbItsLadder()
        {
            Spend(30);

            Assert.That(_refills.RefillWithGems(T0), Is.EqualTo(RefillResult.Refilled));
            Assert.That(_pool.Amount, Is.EqualTo(170));
            Assert.That(_refills.RefillWithGems(T0), Is.EqualTo(RefillResult.AlreadyFull));
            Spend(100);
            Assert.That(_refills.GemCost(T0), Is.EqualTo(200));
            Assert.That(_refills.RefillWithGems(T0), Is.EqualTo(RefillResult.Refilled));
            Spend(200);
            Assert.That(_refills.RefillWithGems(T0), Is.EqualTo(RefillResult.NoneLeft));
            Assert.That(_refills.GemCost(T0 + 24 * HOUR), Is.EqualTo(100));
        }

        [Test]
        public void TheVideo_ShouldBeOfferedBelowHalf_AndWaitItsCooldownAfterAClaim()
        {
            _refills.Refresh(T0);
            Assert.That(_refills.Pending, Is.False);

            Spend(60);
            _refills.Refresh(T0);
            Assert.That(_refills.Pending, Is.True);
            Assert.That(_refills.ClaimAd(T0), Is.EqualTo(ClaimAdResult.Claimed));
            Assert.That(_pool.Amount, Is.EqualTo(140));

            Spend(120);
            _refills.Refresh(T0 + 5 * 60_000);
            Assert.That(_refills.Pending, Is.False);
            _refills.Refresh(T0 + 21 * 60_000);
            Assert.That(_refills.Pending, Is.True);
        }

        [Test]
        public void TheVideos_ShouldStopForTheDay_AtTheirLimit()
        {
            Spend(60);
            _refills.Refresh(T0);
            _refills.ClaimAd(T0);
            Spend(130);
            _refills.Refresh(T0 + HOUR);
            Assert.That(_refills.ClaimAd(T0 + HOUR), Is.EqualTo(ClaimAdResult.Claimed));
            Spend(100);

            _refills.Refresh(T0 + 2 * HOUR);
            Assert.That(_refills.Pending, Is.False);
            Assert.That(_refills.WatchedLeft(T0 + 2 * HOUR), Is.EqualTo(0));
            Assert.That(_refills.WatchedLeft(T0 + 24 * HOUR), Is.EqualTo(2));
        }
    }
}
