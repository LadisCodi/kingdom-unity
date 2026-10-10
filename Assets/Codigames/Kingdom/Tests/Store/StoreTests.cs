using System;
using System.Collections.Generic;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Store;
using Codigames.Kingdom.Store.State;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Store
{
    public class StoreTests
    {
        private sealed class Product : IProductDefinition
        {
            public string Id { get; set; }
            public string DisplayName { get; set; } = "";
            public string Description { get; set; } = "";
            public ProductShelf Shelf { get; set; } = ProductShelf.Gems;
            public double PriceUsd { get; set; }
            public int Gems { get; set; }
            public IReadOnlyDictionary<string, int> Items { get; set; } = new Dictionary<string, int>();
            public string Hero { get; set; }
            public int Builders { get; set; }
            public int Explorers { get; set; }
            public int HeroSlots { get; set; }
            public int NextDayGems { get; set; }
            public int NextDayHeroXp { get; set; }
            public int NextDayFragments { get; set; }
            public IReadOnlyDictionary<string, int> NextDayItems { get; set; } = new Dictionary<string, int>();
            public string OpensOn { get; set; } = "always";
            public string Door { get; set; }
            public string After { get; set; }
            public int Townhall { get; set; }
            public double Hours { get; set; }
            public int Limit { get; set; }
            public double CooldownHours { get; set; }
            public bool Splash { get; set; }
            public bool Widget { get; set; }
        }

        private sealed class Payer : IPayerSettings
        {
            public double MonthlyUsd(PayerProfile profile) => profile switch
            {
                PayerProfile.Minnow => 10,
                PayerProfile.Dolphin => 50,
                _ => 0,
            };

            public int DailyOffers => 3;
            public double OfferSpacingHours => 20;
        }

        private static readonly double JAN_10 = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        private static readonly double FEB_02 = new DateTimeOffset(2026, 2, 2, 8, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

        private CityFixture _city;
        private PayerState _state;
        private Kingdom.City.Builders _builders;
        private Kingdom.Store.Store _store;

        [SetUp]
        public void SetUp()
        {
            _city = new CityFixture();
            _state = new PayerState();
            _builders = new Kingdom.City.Builders(_city.City, _city.Settings, _city.Treasury);
            _store = new Kingdom.Store.Store(_state, new Catalog<IProductDefinition>(new IProductDefinition[]
            {
                new Product { Id = "Pouch", PriceUsd = 0.99, Gems = 500 },
                new Product { Id = "Chest", PriceUsd = 9.99, Gems = 6000 },
                new Product { Id = "Crew", Shelf = ProductShelf.Bag, PriceUsd = 4.99, Gems = 100, Builders = 1 },
                new Product { Id = "Sale", Shelf = ProductShelf.Offer, PriceUsd = 1.99, Gems = 1000 },
            }), new Payer(), _city.Treasury, builders: _builders);
        }

        [Test]
        public void Buy_WithoutAProfile_ShouldBeRefused()
        {
            Assert.That(_store.Buy("Pouch", JAN_10), Is.EqualTo(BuyResult.NoProfile));
            Assert.That(_city.Treasury.Get("Gems"), Is.EqualTo(500));
        }

        [Test]
        public void ChooseProfile_ShouldBeFinal()
        {
            Assert.That(_store.ChooseProfile(PayerProfile.Minnow, JAN_10), Is.EqualTo(ChooseProfileResult.Chosen));
            Assert.That(_store.ChooseProfile(PayerProfile.Dolphin, JAN_10), Is.EqualTo(ChooseProfileResult.AlreadyChosen));
            Assert.That(_store.Profile, Is.EqualTo(PayerProfile.Minnow));
        }

        [Test]
        public void Buy_ShouldSpendTheBudgetAndLandTheGems()
        {
            _store.ChooseProfile(PayerProfile.Minnow, JAN_10);

            Assert.That(_store.Buy("Pouch", JAN_10), Is.EqualTo(BuyResult.Purchased));
            Assert.That(_city.Treasury.Get("Gems"), Is.EqualTo(1000));
            Assert.That(_store.BudgetRemainingCents(JAN_10), Is.EqualTo(901));
            Assert.That(_state.Purchases, Has.Count.EqualTo(1));
        }

        [Test]
        public void Buy_PastTheBudget_ShouldBeCountedAsARefusal()
        {
            _store.ChooseProfile(PayerProfile.Minnow, JAN_10);
            _store.Buy("Chest", JAN_10);

            Assert.That(_store.Buy("Pouch", JAN_10), Is.EqualTo(BuyResult.NoBudget));
            Assert.That(_state.Refusals, Is.EqualTo(1));
            Assert.That(_city.Treasury.Get("Gems"), Is.EqualTo(6500));
        }

        [Test]
        public void TheBudget_ShouldRefillOnTheFirstOfTheMonth()
        {
            _store.ChooseProfile(PayerProfile.Minnow, JAN_10);
            _store.Buy("Chest", JAN_10);

            Assert.That(_store.BudgetRemainingCents(FEB_02), Is.EqualTo(1000));
            Assert.That(_store.Buy("Pouch", FEB_02), Is.EqualTo(BuyResult.Purchased));
            Assert.That(Kingdom.Store.Store.MonthResetsAt(JAN_10),
                Is.EqualTo(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds()));
        }

        [Test]
        public void F2P_ShouldBeRefusedLikeAnyoneShort()
        {
            _store.ChooseProfile(PayerProfile.F2P, JAN_10);

            Assert.That(_store.Buy("Pouch", JAN_10), Is.EqualTo(BuyResult.NoBudget));
        }

        [Test]
        public void AnOffer_WithoutTheOffersRules_ShouldNotBeOnSale()
        {
            _store.ChooseProfile(PayerProfile.Dolphin, JAN_10);

            Assert.That(_store.Buy("Sale", JAN_10), Is.EqualTo(BuyResult.NotOnSale));
            Assert.That(_store.BudgetRemainingCents(JAN_10), Is.EqualTo(5000));
        }

        [Test]
        public void Buy_ShouldHandOverItsBuilders()
        {
            _store.ChooseProfile(PayerProfile.Dolphin, JAN_10);

            _store.Buy("Crew", JAN_10);

            Assert.That(_city.City.Builders, Is.EqualTo(_city.Settings.StartBuilders + 1));
        }

        [Test]
        public void ABuilderBought_ShouldCostMoreAfterEachOneAndStopAtTheCeiling()
        {
            _city.Treasury.Add("Gems", 100_000);

            Assert.That(_builders.GemCost, Is.EqualTo(2500));
            Assert.That(_builders.Buy(), Is.EqualTo(BuyBuilderResult.Purchased));
            Assert.That(_builders.GemCost, Is.EqualTo(5000));
            _builders.Buy();
            _builders.Buy();

            Assert.That(_builders.Buy(), Is.EqualTo(BuyBuilderResult.AtMax));
            Assert.That(_city.City.Builders, Is.EqualTo(4));
        }
    }
}
