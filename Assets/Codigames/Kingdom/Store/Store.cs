using System;
using System.Collections.Generic;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Store.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Store
{
    public enum ChooseProfileResult
    {
        Chosen,
        AlreadyChosen,
    }

    public enum BuyResult
    {
        Purchased,
        NoProfile,
        NoBudget,
        NotOnSale,
    }

    // Is an offer or a daily on sale now? The offers' own rules answer it; without them nothing on those shelves is.
    public interface IOfferShelf
    {
        bool OnSale(IProductDefinition product, double now);
        void Bought(IProductDefinition product, double now);
    }

    // THE SIMULATED STORE (the web's store.ts): nothing here takes money. What it takes is the payer's monthly budget,
    // chosen once per kingdom, and that budget is the instrument — buying one thing means not buying another. The
    // budget is per calendar month (UTC), read off the time given, never rolled over; a tap it cannot cover is
    // counted, since unmet demand at a price is what the store is there to measure. F2P is a budget of nothing: it
    // walks the same path and is refused the same way. A purchase lands its Gems in the wallet for real, then what
    // else it hands over.
    public class Store
    {
        private const string GEMS = "Gems";

        private readonly PayerState _state;
        private readonly ICatalog<IProductDefinition> _products;
        private readonly IPayerSettings _settings;
        private readonly ITreasury _treasury;
        private readonly Bag.Bag _bag;
        private readonly Heroes.Heroes _heroes;
        private readonly ICatalog<Heroes.IBannerDefinition> _banners;
        private readonly Builders _builders;
        private IOfferShelf _offers;

        public Store(PayerState state, ICatalog<IProductDefinition> products, IPayerSettings settings, ITreasury treasury,
            Bag.Bag bag = null, Heroes.Heroes heroes = null, ICatalog<Heroes.IBannerDefinition> banners = null, Builders builders = null)
        {
            _state = state;
            _products = products;
            _settings = settings;
            _treasury = treasury;
            _bag = bag;
            _heroes = heroes;
            _banners = banners;
            _builders = builders;
        }

        // A purchase went through, or a tap was refused.
        public event Action<IProductDefinition, BuyResult> Bought;

        public IReadOnlyList<IProductDefinition> All => _products.Items;
        public IProductDefinition Get(string sku) => _products.Get(sku);

        // The offers' rules, once they exist: they answer for the offer and daily shelves.
        public void UseOffers(IOfferShelf offers) => _offers = offers;

        // ---- the payer

        public PayerProfile? Profile => _state.Profile;
        public PayerState Payer => _state;

        public ChooseProfileResult ChooseProfile(PayerProfile profile, double now)
        {
            if (_state.Profile != null) return ChooseProfileResult.AlreadyChosen;
            _state.Profile = profile;
            _state.ChosenAt = now;
            _state.MonthIndex = MonthIndex(now);
            _state.SpentCentsThisMonth = 0;
            return ChooseProfileResult.Chosen;
        }

        public int MonthlyBudgetCents(PayerProfile profile) => (int)Math.Round(_settings.MonthlyUsd(profile) * 100, MidpointRounding.AwayFromZero);

        // What is left this month; null before a profile. A later month reads as a full budget.
        public int? BudgetRemainingCents(double now)
        {
            if (_state.Profile is not { } profile) return null;
            var spent = MonthIndex(now) == _state.MonthIndex ? _state.SpentCentsThisMonth : 0;
            return Math.Max(0, MonthlyBudgetCents(profile) - spent);
        }

        public static int PriceCents(IProductDefinition product) => (int)Math.Round(product.PriceUsd * 100, MidpointRounding.AwayFromZero);

        public bool CanAfford(IProductDefinition product, double now) => BudgetRemainingCents(now) is { } left && left >= PriceCents(product);

        // Months since the epoch, UTC: January 1970 is 0.
        public static int MonthIndex(double now)
        {
            var date = DateTimeOffset.FromUnixTimeMilliseconds((long)now).UtcDateTime;
            return date.Year * 12 + date.Month - 1;
        }

        // The first of next month, 00:00 UTC: when the budget refills.
        public static double MonthResetsAt(double now)
        {
            var date = DateTimeOffset.FromUnixTimeMilliseconds((long)now).UtcDateTime;
            var next = new DateTimeOffset(date.Year, date.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1);
            return next.ToUnixTimeMilliseconds();
        }

        // ---- the shelves

        // The Gem packs and the Bag's bundles always are; an offer and a daily as the offers say; the Survey never here.
        public bool OnSale(IProductDefinition product, double now) => product.Shelf switch
        {
            ProductShelf.Gems or ProductShelf.Bag => true,
            ProductShelf.Offer or ProductShelf.Daily => _offers != null && _offers.OnSale(product, now),
            _ => false,
        };

        // Buy any product but the Survey: the budget, then everything it hands over.
        public BuyResult Buy(string sku, double now)
        {
            var product = _products.Get(sku);
            if (!OnSale(product, now)) return BuyResult.NotOnSale;
            var paid = Pay(product, now);
            if (paid == BuyResult.Purchased)
            {
                Hand(product);
                _offers?.Bought(product, now);
            }

            Bought?.Invoke(product, paid);
            return paid;
        }

        // The price leaves the month's budget and the Gems land; a refusal is counted.
        private BuyResult Pay(IProductDefinition product, double now)
        {
            if (_state.Profile is not { } profile) return BuyResult.NoProfile;
            var month = MonthIndex(now);
            if (month != _state.MonthIndex)
            {
                _state.MonthIndex = month;
                _state.SpentCentsThisMonth = 0;
            }

            var cents = PriceCents(product);
            if (MonthlyBudgetCents(profile) - _state.SpentCentsThisMonth < cents)
            {
                _state.Refusals++;
                return BuyResult.NoBudget;
            }

            _state.SpentCentsThisMonth += cents;
            _state.Purchases.Add(new Purchase { Sku = product.Id, PriceCents = cents, At = now });
            if (product.Gems > 0) _treasury.Add(GEMS, product.Gems);
            return BuyResult.Purchased;
        }

        // Items into the Bag, the hero (a duplicate pays the fragments of the banner most generous with its rarity),
        // builders up to the ceiling, hero slots for good.
        private void Hand(IProductDefinition product)
        {
            if (_bag != null)
                foreach (var item in product.Items)
                    _bag.Grant(item.Key, item.Value);
            if (_heroes != null && !string.IsNullOrEmpty(product.Hero)) _heroes.Grant(product.Hero, DuplicateFragments(product.Hero));
            for (var i = 0; i < product.Builders; i++) _builders?.Grant();
            if (product.HeroSlots > 0) _heroes?.GrantSlots(product.HeroSlots);
        }

        private int DuplicateFragments(string hero)
        {
            if (_banners == null) return 0;
            var rarity = _heroes.Get(hero).Rarity;
            var best = 0;
            foreach (var banner in _banners.Items)
                if (banner.Weight(rarity) > 0) best = Math.Max(best, banner.DuplicateFragments);
            return best;
        }
    }
}
