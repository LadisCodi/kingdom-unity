using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Store.State;
using Codigames.Modules.Core;
using Codigames.Modules.Randomness;

namespace Codigames.Kingdom.Store
{
    public enum ClaimNextDayResult
    {
        Claimed,
        NotYet,
        Nothing,
    }

    // THE STORE'S OFFERS (the web's offers.ts): packs not always on the shelf. An offer has a window — opened, closes,
    // sold — and something that opens it: always, a door, the step before it in a chain bought (the next day, UTC), a
    // Townhall level, a need just felt (Mana low or out, explorers out, heroes benched, a build refused for want of a
    // builder). A window that closes is read against the time given, never scheduled: an offer produces nothing, so it
    // is no boundary of the timeline, and only the live tick opens them — a need met in a replayed absence opens nothing.
    // Offers are spaced: one that opens on its own waits while another opened less than the spacing ago; one answering
    // a need does not wait. None opens below its Townhall level, nor while a slot it opens would not fit. The daily offers
    // are drawn, not opened: so many of the pool a day, by a hash of the day and the product. A product's next-day part
    // waits for the next day and the player's claim.
    public class Offers : IOfferShelf
    {
        private const double HOUR_MS = 3_600_000;
        private const double DAY_MS = 86_400_000;
        private const string GEMS = "Gems";
        private const string HERO_XP = "HeroXp";
        private static readonly HashSet<string> NEEDS = new() { "manaLow", "manaOut", "buildersBusy", "explorersBusy", "heroesBenched" };

        private readonly OffersState _state;
        private readonly PayerState _payer;
        private readonly ICatalog<IProductDefinition> _products;
        private readonly IPayerSettings _settings;
        private readonly IOfferContext _context;
        private readonly uint _seed;
        private readonly ITreasury _treasury;
        private readonly Bag.Bag _bag;
        private readonly Heroes.Heroes _heroes;

        public Offers(OffersState state, PayerState payer, ICatalog<IProductDefinition> products, IPayerSettings settings, IOfferContext context,
            uint seed, ITreasury treasury, Bag.Bag bag = null, Heroes.Heroes heroes = null)
        {
            _state = state;
            _payer = payer;
            _products = products;
            _settings = settings;
            _context = context;
            _seed = seed;
            _treasury = treasury;
            _bag = bag;
            _heroes = heroes;
        }

        // An offer's window opened.
        public event Action<IProductDefinition> Opened;

        // A window, a draw or a next-day part moved.
        public event Action Changed;

        private IEnumerable<IProductDefinition> Shelf(ProductShelf shelf) => _products.Items.Where(p => p.Shelf == shelf);

        // A trigger that comes back once its window has closed and cooled.
        private static bool Repeats(IProductDefinition p) => p.OpensOn == "townhall" || NEEDS.Contains(p.OpensOn);

        public static long DayIndex(double now) => (long)Math.Floor(now / DAY_MS);

        // ---- windows

        public OfferWindow Window(string sku) => _state.Windows.TryGetValue(sku, out var w) ? w : null;

        public bool OfferOn(IProductDefinition product, double now)
        {
            var w = Window(product.Id);
            if (w == null || now < w.Opened) return false;
            if (w.Closes != null && now >= w.Closes) return false;
            if (product.Limit > 0 && w.Bought >= product.Limit) return false;
            return _context.SlotsFit(product);
        }

        // The offers on sale now, in shelf order.
        public IReadOnlyList<IProductDefinition> OffersOn(double now) => Shelf(ProductShelf.Offer).Where(p => OfferOn(p, now)).ToList();

        private bool MayOpen(IProductDefinition product, double now)
        {
            if (!_context.IsDoorOpen("store")) return false;
            if (_context.TownhallLevel < product.Townhall) return false;
            if (!_context.SlotsFit(product)) return false;
            var w = Window(product.Id);
            if (w == null) return true;
            if (!Repeats(product) || w.Closes == null) return false;
            return now >= w.Closes + product.CooldownHours * HOUR_MS;
        }

        private void Open(IProductDefinition product, double now)
        {
            _state.Windows[product.Id] = new OfferWindow { Opened = now, Closes = product.Hours > 0 ? now + product.Hours * HOUR_MS : null };
            Opened?.Invoke(product);
            Changed?.Invoke();
        }

        private double? LastBought(string sku)
        {
            for (var i = _payer.Purchases.Count - 1; i >= 0; i--)
                if (_payer.Purchases[i].Sku == sku) return _payer.Purchases[i].At;
            return null;
        }

        private bool TooSoon(double now)
        {
            var gap = _settings.OfferSpacingHours * HOUR_MS;
            return _state.Windows.Values.Any(w => now < w.Opened + gap);
        }

        // The latch, from the live tick: every offer whose trigger is met and whose window may open — a need at once,
        // the rest one at a time and spaced. A Townhall level met while waiting its turn is kept for the next.
        public void Refresh(double now)
        {
            var level = _context.TownhallLevel;
            var raised = level > _state.Townhall;
            var held = false;
            foreach (var product in Shelf(ProductShelf.Offer))
            {
                var boughtAt = product.After == null ? null : LastBought(product.After);
                var met = product.OpensOn switch
                {
                    "always" => true,
                    "door" => product.Door != null && _context.IsDoorOpen(product.Door),
                    "after" => boughtAt != null && now >= (DayIndex(boughtAt.Value) + 1) * DAY_MS,
                    "townhall" => raised && level >= product.Townhall,
                    "manaLow" or "manaOut" or "explorersBusy" or "heroesBenched" => _context.Feels(product.OpensOn),
                    _ => false,
                };
                if (!met || !MayOpen(product, now)) continue;
                if (!NEEDS.Contains(product.OpensOn) && TooSoon(now))
                {
                    if (product.OpensOn == "townhall") held = true;
                    continue;
                }

                Open(product, now);
            }

            if (!held) _state.Townhall = level;
        }

        // A moment the live game reports rather than the tick sees: a build refused for want of a builder.
        public IReadOnlyList<IProductDefinition> Trigger(string trigger, double now)
        {
            var opened = new List<IProductDefinition>();
            foreach (var product in Shelf(ProductShelf.Offer))
            {
                if (product.OpensOn != trigger || !MayOpen(product, now)) continue;
                Open(product, now);
                opened.Add(product);
            }

            return opened;
        }

        // ---- the store's questions

        public bool OnSale(IProductDefinition product, double now)
            => product.Shelf == ProductShelf.Offer ? OfferOn(product, now) : product.Shelf == ProductShelf.Daily && DailyOn(product, now);

        // A purchase: counted in its window, and its next-day part scheduled.
        public void Bought(IProductDefinition product, double now)
        {
            if (product.Shelf == ProductShelf.Offer && Window(product.Id) is { } w) w.Bought++;
            if (HasNextDay(product)) _state.NextDay.Add(new NextDayDelivery { Sku = product.Id, ClaimableAt = (DayIndex(now) + 1) * DAY_MS });
            Changed?.Invoke();
        }

        // ---- the next day

        public static bool HasNextDay(IProductDefinition p)
            => p.NextDayGems + p.NextDayHeroXp + p.NextDayFragments > 0 || p.NextDayItems.Values.Any(n => n > 0);

        public IReadOnlyList<NextDayDelivery> NextDayReady(double now) => _state.NextDay.Where(d => now >= d.ClaimableAt).ToList();

        public IReadOnlyList<NextDayDelivery> NextDayWaiting(double now) => _state.NextDay.Where(d => now < d.ClaimableAt).ToList();

        // Its Gems, Hero XP, fragments of its hero and items.
        public ClaimNextDayResult ClaimNextDay(string sku, double now)
        {
            var at = _state.NextDay.FindIndex(d => d.Sku == sku);
            if (at < 0) return ClaimNextDayResult.Nothing;
            if (now < _state.NextDay[at].ClaimableAt) return ClaimNextDayResult.NotYet;
            _state.NextDay.RemoveAt(at);
            var p = _products.Get(sku);
            if (p.NextDayGems > 0) _treasury.Add(GEMS, p.NextDayGems);
            if (p.NextDayHeroXp > 0)
            {
                if (_heroes != null) _heroes.AddXp(p.NextDayHeroXp);
                else _treasury.Add(HERO_XP, p.NextDayHeroXp);
            }

            if (p.Hero != null && p.NextDayFragments > 0) _heroes?.AddFragments(p.Hero, p.NextDayFragments);
            if (_bag != null)
                foreach (var (id, n) in p.NextDayItems)
                    _bag.Grant(id, n);
            Changed?.Invoke();
            return ClaimNextDayResult.Claimed;
        }

        // ---- the daily offers

        public static double DailyResetsAt(double now) => (DayIndex(now) + 1) * DAY_MS;

        // So many of the pool, the Townhall permitting, the same draw however often it is asked.
        public IReadOnlyList<IProductDefinition> DailyOffers(double now)
        {
            if (!_context.IsDoorOpen("store")) return Array.Empty<IProductDefinition>();
            var day = DayIndex(now);
            var level = _context.TownhallLevel;
            var pool = Shelf(ProductShelf.Daily).ToList();
            return pool.Where(p => p.Townhall <= level)
                .Select(p => (Product: p, Roll: Rand.Value(_seed, "daily", day, p.Id)))
                .OrderBy(d => d.Roll)
                .Take(_settings.DailyOffers)
                .Select(d => d.Product)
                .OrderBy(p => pool.IndexOf(p))
                .ToList();
        }

        public int BoughtToday(string sku, double now)
        {
            var day = DayIndex(now);
            return _payer.Purchases.Count(p => p.Sku == sku && DayIndex(p.At) == day);
        }

        public bool DailyOn(IProductDefinition product, double now)
            => DailyOffers(now).Contains(product) && (product.Limit == 0 || BoughtToday(product.Id, now) < product.Limit);
    }
}
