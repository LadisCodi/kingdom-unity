using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Store;
using Codigames.Kingdom.Store.State;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Store
{
    public class OffersTests
    {
        private sealed class Product : IProductDefinition
        {
            public string Id { get; set; }
            public string DisplayName { get; set; } = "";
            public string Description { get; set; } = "";
            public ProductShelf Shelf { get; set; } = ProductShelf.Offer;
            public double PriceUsd { get; set; } = 4.99;
            public int Gems { get; set; } = 100;
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
            public double MonthlyUsd(PayerProfile profile) => 1000;
            public int DailyOffers => 2;
            public double OfferSpacingHours => 20;
        }

        private sealed class Context : IOfferContext
        {
            public HashSet<string> Doors { get; } = new() { "store" };
            public HashSet<string> Needs { get; } = new();
            public int TownhallLevel { get; set; } = 1;
            public bool Fits { get; set; } = true;
            public bool IsDoorOpen(string door) => Doors.Contains(door);
            public bool Feels(string need) => Needs.Contains(need);
            public bool SlotsFit(IProductDefinition product) => Fits;
        }

        private const double HOUR = 3_600_000;
        private static readonly double T0 = new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

        private CityFixture _city;
        private Context _context;
        private PayerState _payer;
        private OffersState _state;
        private Offers _offers;
        private Kingdom.Store.Store _store;

        private void Make(params IProductDefinition[] products)
        {
            _city = new CityFixture();
            _context = new Context();
            _payer = new PayerState();
            _state = new OffersState();
            var catalog = new Catalog<IProductDefinition>(products);
            _offers = new Offers(_state, _payer, catalog, new Payer(), _context, 7, _city.Treasury);
            _store = new Kingdom.Store.Store(_payer, catalog, new Payer(), _city.Treasury);
            _store.UseOffers(_offers);
            _store.ChooseProfile(PayerProfile.Whale, T0);
        }

        [Test]
        public void AnAlwaysOffer_ShouldOpenOnTheFirstRefresh_AndCloseAtItsHours()
        {
            var sale = new Product { Id = "Sale", Hours = 24 };
            Make(sale);

            _offers.Refresh(T0);

            Assert.That(_offers.OfferOn(sale, T0 + 23 * HOUR), Is.True);
            Assert.That(_offers.OfferOn(sale, T0 + 24 * HOUR), Is.False);
        }

        [Test]
        public void Offers_ShouldBeSpaced_ButANeedShouldNotWait()
        {
            var first = new Product { Id = "First" };
            var second = new Product { Id = "Second" };
            var mana = new Product { Id = "Mana", OpensOn = "manaOut", Hours = 24 };
            Make(first, second, mana);
            _context.Needs.Add("manaOut");

            _offers.Refresh(T0);

            Assert.That(_offers.Window("First"), Is.Not.Null);
            Assert.That(_offers.Window("Second"), Is.Null);
            Assert.That(_offers.Window("Mana"), Is.Not.Null);
            _offers.Refresh(T0 + 20 * HOUR);
            Assert.That(_offers.Window("Second"), Is.Not.Null);
        }

        [Test]
        public void NothingShouldOpen_BehindTheStoreDoor_OrBelowItsTownhall()
        {
            var sale = new Product { Id = "Sale", Townhall = 3 };
            Make(sale);
            _context.Doors.Clear();

            _offers.Refresh(T0);
            Assert.That(_offers.Window("Sale"), Is.Null);

            _context.Doors.Add("store");
            _offers.Refresh(T0);
            Assert.That(_offers.Window("Sale"), Is.Null);

            _context.TownhallLevel = 3;
            _offers.Refresh(T0);
            Assert.That(_offers.Window("Sale"), Is.Not.Null);
        }

        [Test]
        public void TheNextStepOfAChain_ShouldOpenTheDayAfterTheStepBeforeIsBought()
        {
            var one = new Product { Id = "One" };
            var two = new Product { Id = "Two", OpensOn = "after", After = "One" };
            Make(one, two);
            _offers.Refresh(T0);

            Assert.That(_store.Buy("One", T0), Is.EqualTo(BuyResult.Purchased));
            _offers.Refresh(T0 + 2 * HOUR);
            Assert.That(_offers.Window("Two"), Is.Null);

            _offers.Refresh(T0 + 21 * HOUR);
            Assert.That(_offers.Window("Two"), Is.Not.Null);
        }

        [Test]
        public void ALimit_ShouldSellOut()
        {
            var sale = new Product { Id = "Sale", Limit = 1 };
            Make(sale);
            _offers.Refresh(T0);

            Assert.That(_store.Buy("Sale", T0), Is.EqualTo(BuyResult.Purchased));
            Assert.That(_store.Buy("Sale", T0), Is.EqualTo(BuyResult.NotOnSale));
        }

        [Test]
        public void ABuilderRefused_ShouldOpenItsOffer_AndItShouldComeBackAfterItsCooldown()
        {
            var crew = new Product { Id = "Crew", OpensOn = "buildersBusy", Hours = 48, CooldownHours = 24 };
            Make(crew);

            Assert.That(_offers.Trigger("buildersBusy", T0), Has.Count.EqualTo(1));
            Assert.That(_offers.Trigger("buildersBusy", T0 + 60 * HOUR), Is.Empty);
            Assert.That(_offers.Trigger("buildersBusy", T0 + 72 * HOUR), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThePartForTheNextDay_ShouldWaitForItsDay_ThenBeClaimedOnce()
        {
            var pack = new Product { Id = "Pack", Gems = 100, NextDayGems = 300 };
            Make(pack);
            _offers.Refresh(T0);
            _store.Buy("Pack", T0);

            Assert.That(_offers.ClaimNextDay("Pack", T0 + HOUR), Is.EqualTo(ClaimNextDayResult.NotYet));
            Assert.That(_offers.ClaimNextDay("Pack", T0 + 15 * HOUR), Is.EqualTo(ClaimNextDayResult.Claimed));
            Assert.That(_city.Treasury.Get("Gems"), Is.EqualTo(500 + 100 + 300));
            Assert.That(_offers.ClaimNextDay("Pack", T0 + 16 * HOUR), Is.EqualTo(ClaimNextDayResult.Nothing));
        }

        [Test]
        public void TheDailyDraw_ShouldBeTheSameAllDay_AndRespectItsLimit()
        {
            var pool = Enumerable.Range(0, 6).Select(i => (IProductDefinition)new Product { Id = "D" + i, Shelf = ProductShelf.Daily, Limit = 1 }).ToArray();
            Make(pool);

            var morning = _offers.DailyOffers(T0).Select(p => p.Id).ToList();
            var evening = _offers.DailyOffers(T0 + 12 * HOUR).Select(p => p.Id).ToList();

            Assert.That(morning, Has.Count.EqualTo(2));
            Assert.That(evening, Is.EqualTo(morning));
            Assert.That(_store.Buy(morning[0], T0), Is.EqualTo(BuyResult.Purchased));
            Assert.That(_store.Buy(morning[0], T0), Is.EqualTo(BuyResult.NotOnSale));
        }
    }
}
