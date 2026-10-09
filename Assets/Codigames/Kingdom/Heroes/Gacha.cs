using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Battles;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Heroes.State;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;
using Codigames.Modules.Randomness;

namespace Codigames.Kingdom.Heroes
{
    public enum PullOutcome
    {
        Pulled,
        NotEnoughKeys,
        NothingToPull,
    }

    public enum FreePullOutcome
    {
        Pulled,
        NoneLeft,
        OnCooldown,
        NoFreePulls,
    }

    public enum LootKind
    {
        Fragments,
        Currency,
        Item,
    }

    // Which of a call's three slots a loot row fills (Docs/features/10-heroes.md §6.4).
    public enum LootSlot
    {
        Hero,
        HeroGoods,
        Supplies,
    }

    // One prize a call drew from its banner's loot table, as paid: a hero's Fragments, a currency or an item.
    public readonly struct CallLoot
    {
        public CallLoot(LootKind kind, string id, int amount)
        {
            Kind = kind;
            Id = id;
            Amount = amount;
        }

        public LootKind Kind { get; }
        public string Id { get; }
        public int Amount { get; }
    }

    // One call: the hero it brought (null on a miss) and its rarity, whether they were owned already and paid
    // Fragments instead, the loot, and whether a guarantee delivered it.
    public sealed class PullResult
    {
        public PullOutcome Outcome { get; set; }
        public string HeroId { get; set; }
        public HeroRarity? Rarity { get; set; }
        public bool Duplicate { get; set; }
        public int Fragments { get; set; }
        public string FragmentsOf { get; set; }
        public IReadOnlyList<CallLoot> Loot { get; set; } = Array.Empty<CallLoot>();
        public bool Guaranteed { get; set; }
        public bool GuaranteedLegendary { get; set; }
    }

    // The banners' calls (Docs/features/10-heroes.md §6): a key a call, the first standard call free; a hero by the
    // chance ladder and the soft and hard pity, of a rarity by the banner's weights, from the bag — the owned and a few
    // open strangers per rarity; three loot slots always paid; free calls an ad pays for, a few a UTC day. Every roll is
    // a hash of (banner, call number, slot), never a stream.
    public class Gacha
    {
        public const string STANDARD = "basic";
        private const double DAY_MS = 86_400_000;
        private static readonly HeroRarity[] RARITIES = { HeroRarity.Common, HeroRarity.Rare, HeroRarity.Legendary };

        private readonly GachaState _state;
        private readonly Heroes _heroes;
        private readonly ICatalog<IBannerDefinition> _banners;
        private readonly IHeroLadderSettings _ladder;
        private readonly IItemHoldings _items;
        private readonly ITreasury _treasury;
        private readonly IBonuses _bonuses;
        private readonly uint _seed;

        public Gacha(GachaState state, Heroes heroes, ICatalog<IBannerDefinition> banners, IItemHoldings items, ITreasury treasury, uint seed,
            IBonuses bonuses = null)
        {
            _state = state;
            _heroes = heroes;
            _banners = banners;
            _ladder = heroes.Ladder.Settings;
            _items = items;
            _treasury = treasury;
            _seed = seed;
            _bonuses = bonuses;
        }

        // A call was made: counters, keys, heroes, Fragments or the free allowance moved.
        public event Action Changed;

        public IReadOnlyList<IBannerDefinition> Banners => _banners.Items;
        public IBannerDefinition Get(string banner) => _banners.Get(banner);

        // ---- the price

        // One key of the banner's kind; the first standard call is free.
        public (string Key, int Amount) PullPrice(string banner = STANDARD)
            => (Get(banner).Key, banner == STANDARD && PullCount(banner) == 0 ? 0 : 1);

        public int PullCount(string banner) => _state.PullCounts.TryGetValue(banner, out var n) ? n : 0;

        // Calls since the last hero; the counter the screen always shows.
        public int PityCount(string banner) => _state.PityCounters.TryGetValue(banner, out var n) ? n : 0;

        public int LegendaryPityCount(string banner) => _state.LegendaryPity.TryGetValue(banner, out var n) ? n : 0;

        public int PullsToGuarantee(string banner) => Math.Max(0, Get(banner).HardPityAt - PityCount(banner));

        // Null on a banner with no Legendary guarantee.
        public int? PullsToLegendary(string banner)
            => Get(banner).LegendaryPityAt == 0 ? null : Math.Max(0, Get(banner).LegendaryPityAt - LegendaryPityCount(banner));

        // ---- the chance

        // Before pity: a ladder by heroes owned, the last rung for ever after.
        public double BaseHeroChance(string banner = STANDARD)
        {
            var ladder = Get(banner).HeroChanceByOwned;
            return ladder.Count == 0 ? 0 : ladder[Math.Min(_heroes.Owned.Count, ladder.Count - 1)];
        }

        // This call's chance: soft pity ramps from the base to certain between its two marks; hard pity is a promise.
        public double HeroChanceAt(string banner = STANDARD, int? pity = null)
        {
            var b = Get(banner);
            var p = pity ?? PityCount(banner);
            var chance = BaseHeroChance(banner);
            if (p >= b.HardPityAt - 1) return 1;
            if (p < b.SoftPityAt) return chance;
            var span = Math.Max(1, b.HardPityAt - b.SoftPityAt);
            return Math.Min(1, chance + (1 - chance) * ((double)(p - b.SoftPityAt) / span));
        }

        // The rarities this banner can roll: a weight of 0 keeps one off.
        public IReadOnlyList<HeroRarity> BannerRarities(string banner) => RARITIES.Where(r => Get(banner).Weight(r) > 0).ToList();

        // ---- the bag (§6.6): derived, never stored

        // The order a rarity's heroes open in: the ranked by rank, then the rest shuffled per kingdom.
        public IReadOnlyList<string> BagOrder(HeroRarity rarity)
        {
            var all = OfRarity(rarity).ToList();
            var ranked = all.Where(h => h.BagRank != null).OrderBy(h => h.BagRank.Value).Select(h => h.Id);
            var shuffled = all.Where(h => h.BagRank == null).OrderBy(h => Rand.Value(_seed, "heroBag", h.Id)).Select(h => h.Id);
            return ranked.Concat(shuffled).ToList();
        }

        // The strangers a call can reach: the first few of the bag's order, any already holding Fragments, and a
        // season hero while its banner leans toward them.
        public IReadOnlyList<string> OpenHeroes(HeroRarity rarity)
        {
            var missing = BagOrder(rarity).Where(id => !_heroes.Owns(id)).ToList();
            var first = new HashSet<string>(missing.Take(_ladder.BagOpen(rarity)));
            var featured = _banners.Items.Select(b => b.FeaturedHero).Where(id => !string.IsNullOrEmpty(id)).ToHashSet();
            return missing.Where(id => first.Contains(id) || _heroes.Fragments(id) > 0 || featured.Contains(id)).ToList();
        }

        // Everyone of a rarity in the bag: the owned, then the open.
        public IReadOnlyList<string> BagHeroes(HeroRarity rarity)
            => OfRarity(rarity).Where(h => _heroes.Owns(h.Id)).Select(h => h.Id).Concat(OpenHeroes(rarity)).ToList();

        // Who a hit at this rarity hands over: an open hero; once the rarity is complete, a duplicate.
        public IReadOnlyList<string> BannerPool(string banner, HeroRarity rarity)
        {
            if (Get(banner).Weight(rarity) <= 0) return Array.Empty<string>();
            var open = OpenHeroes(rarity);
            return open.Count > 0 ? open : BagHeroes(rarity);
        }

        // The banner's share of the bag, at every rarity it rolls.
        public IReadOnlyList<string> BannerHeroes(string banner) => BannerRarities(banner).SelectMany(BagHeroes).ToList();

        // A weighted draw, unless the Legendary guarantee is due.
        public HeroRarity RarityFor(string banner, double roll)
        {
            var b = Get(banner);
            var rarities = BannerRarities(banner);
            if (b.LegendaryPityAt > 0 && LegendaryPityCount(banner) >= b.LegendaryPityAt - 1) return HeroRarity.Legendary;
            var cut = roll * rarities.Sum(b.Weight);
            foreach (var r in rarities)
            {
                cut -= b.Weight(r);
                if (cut < 0) return r;
            }

            return rarities[rarities.Count - 1];
        }

        // ---- free calls, for an ad

        public int FreePullsLeft(string banner, double now)
        {
            var used = _state.FreePulls.TryGetValue(banner, out var held) && held.Day == DayIndex(now) ? held.Used : 0;
            return Math.Max(0, Get(banner).FreePerDay - used);
        }

        // When the next free call is offered; 0 when one is offered now. A stamp, not a countdown.
        public double FreePullReadyAt(string banner) => _state.FreePulls.TryGetValue(banner, out var held) ? held.ReadyAt : 0;

        public bool FreePullAvailable(string banner, double now)
            => Get(banner).FreePerDay > 0 && FreePullsLeft(banner, now) > 0 && now >= FreePullReadyAt(banner);

        public FreePullOutcome ClaimFreePull(string banner, double now, out PullResult pull)
        {
            pull = null;
            var b = Get(banner);
            if (b.FreePerDay <= 0) return FreePullOutcome.NoFreePulls;
            if (FreePullsLeft(banner, now) <= 0) return FreePullOutcome.NoneLeft;
            if (now < FreePullReadyAt(banner)) return FreePullOutcome.OnCooldown;
            var ledger = RollFreePulls(banner, now);
            ledger.Used += 1;
            ledger.ReadyAt = now + b.FreeCooldownSeconds * 1000;
            pull = Pull(banner, free: true);
            return FreePullOutcome.Pulled;
        }

        // ---- the call

        // Stardust a call pays: the authored amount, raised by the tree.
        public int CallStardust(double amount) => Combat.JsRound(amount * _bonuses.Multiplier("summonStardust"));

        // Hit or miss against the chance; on a hit the rarity, then the hero from the bag; then the three slots. The
        // standard banner's first call and the first calls across every banner cannot miss, and the latter bring a
        // hero not owned yet.
        public PullResult Pull(string banner = STANDARD, bool free = false)
        {
            var b = Get(banner);
            var price = PullPrice(banner);
            var cost = free ? 0 : price.Amount;
            if (_items.Count(price.Key) < cost) return new PullResult { Outcome = PullOutcome.NotEnoughKeys };
            if (BannerHeroes(banner).Count == 0) return new PullResult { Outcome = PullOutcome.NothingToPull };

            if (cost > 0) _items.Take(price.Key, cost);

            var n = PullCount(banner);
            var firstCall = banner == STANDARD && n == 0;
            var starter = _state.PullCounts.Values.Sum() < _ladder.FirstCallsNewHero;
            var forcedHit = firstCall || starter;
            var pity = PityCount(banner);
            var legPity = LegendaryPityCount(banner);
            _state.PullCounts[banner] = n + 1;

            if (!forcedHit && Rand.Value(_seed, "gacha", banner, n) >= HeroChanceAt(banner, pity))
            {
                // Never a dead call: the hero slot pays a Fragment instead.
                _state.PityCounters[banner] = pity + 1;
                _state.LegendaryPity[banner] = legPity + 1;
                var missLoot = DrawLoot(banner, n, false);
                Changed?.Invoke();
                return new PullResult { Outcome = PullOutcome.Pulled, Loot = missLoot };
            }

            // Read the guarantee before the counters move: RarityFor consults it.
            var forced = b.LegendaryPityAt > 0 && legPity >= b.LegendaryPityAt - 1;
            var rarity = RarityFor(banner, Rand.Value(_seed, "gachaRarity", banner, n));
            var pool = BannerPool(banner, rarity);
            if (pool.Count == 0)
            {
                // A rarity no hero carries yet: one the banner can fill, rather than eating the call.
                foreach (var r in BannerRarities(banner))
                {
                    var alt = BannerPool(banner, r);
                    if (alt.Count == 0) continue;
                    rarity = r;
                    pool = alt;
                    break;
                }
            }

            if (starter && pool.All(_heroes.Owns))
            {
                // This rarity is complete: a starter call moves to one that is not.
                foreach (var r in BannerRarities(banner))
                {
                    var alt = BannerPool(banner, r).Where(id => !_heroes.Owns(id)).ToList();
                    if (alt.Count == 0) continue;
                    rarity = r;
                    pool = alt;
                    break;
                }
            }

            _state.PityCounters[banner] = 0;
            _state.LegendaryPity[banner] = rarity == HeroRarity.Legendary ? 0 : legPity + 1;
            var heroId = pool[(int)Math.Floor(Rand.Value(_seed, "gachaHero", banner, n) * pool.Count)];
            var duplicate = !_heroes.Grant(heroId, b.DuplicateFragments);
            var loot = DrawLoot(banner, n, true);
            Changed?.Invoke();
            return new PullResult
            {
                Outcome = PullOutcome.Pulled,
                HeroId = heroId,
                Rarity = rarity,
                Duplicate = duplicate,
                Fragments = duplicate ? b.DuplicateFragments : 0,
                FragmentsOf = duplicate ? heroId : null,
                Loot = loot,
                Guaranteed = forcedHit || pity >= b.HardPityAt - 1,
                GuaranteedLegendary = forced && rarity == HeroRarity.Legendary,
            };
        }

        // Ten calls at ten keys, all or nothing; the free first call is free inside a batch too.
        public PullOutcome PullMany(string banner, int count, out IReadOnlyList<PullResult> pulls)
        {
            pulls = Array.Empty<PullResult>();
            var price = PullPrice(banner);
            var owed = price.Amount == 0 ? Math.Max(0, count - 1) : count;
            if (_items.Count(price.Key) < owed) return PullOutcome.NotEnoughKeys;
            if (BannerHeroes(banner).Count == 0) return PullOutcome.NothingToPull;
            var list = new List<PullResult>(count);
            for (var i = 0; i < count; i++) list.Add(Pull(banner));
            pulls = list;
            return PullOutcome.Pulled;
        }

        // A call whose hero is decided before it is made (the collection prize's golden call): no price, no loot, no
        // roll and no counter moved; an owned hero pays the banner's duplicate Fragments.
        public PullResult CallGuaranteed(string banner, string heroId)
        {
            var b = Get(banner);
            var duplicate = !_heroes.Grant(heroId, b.DuplicateFragments);
            Changed?.Invoke();
            return new PullResult
            {
                Outcome = PullOutcome.Pulled, HeroId = heroId, Rarity = _heroes.Get(heroId).Rarity, Duplicate = duplicate,
                Fragments = duplicate ? b.DuplicateFragments : 0, FragmentsOf = duplicate ? heroId : null, Guaranteed = true,
            };
        }

        // ---- loot

        public static LootSlot SlotOf(BannerLoot row) => row.Reward switch
        {
            LootReward.Fragments => LootSlot.Hero,
            LootReward.Item => LootSlot.Supplies,
            _ => LootSlot.HeroGoods,
        };

        // Three slots, always: the hero slot a Fragment when the roll missed; the hero-goods slot Stardust or Hero XP,
        // or now and then a second Fragment; the supplies slot an item.
        private IReadOnlyList<CallLoot> DrawLoot(string banner, int n, bool hit)
        {
            var b = Get(banner);
            var paid = new List<CallLoot>(3);

            void Pay(LootSlot slot, int i)
            {
                var row = DrawRow(b.Loot.Where(e => SlotOf(e) == slot).ToList(), Rand.Value(_seed, "gachaLoot", banner, n, i));
                if (row == null) return;
                var loot = PayLoot(banner, row.Value, Rand.Value(_seed, "gachaFrag", banner, n, i));
                if (loot != null) paid.Add(loot.Value);
            }

            if (!hit) Pay(LootSlot.Hero, 0);
            var extra = Rand.Value(_seed, "gachaExtra", banner, n) < b.ExtraHeroSlotChance;
            Pay(extra ? LootSlot.Hero : LootSlot.HeroGoods, 1);
            Pay(LootSlot.Supplies, 2);
            return paid;
        }

        private static BannerLoot? DrawRow(IReadOnlyList<BannerLoot> rows, double roll)
        {
            var live = rows.Where(e => e.Weight > 0).ToList();
            if (live.Count == 0) return null;
            var cut = roll * live.Sum(e => e.Weight);
            foreach (var e in live)
            {
                cut -= e.Weight;
                if (cut < 0) return e;
            }

            return live[live.Count - 1];
        }

        private CallLoot? PayLoot(string banner, BannerLoot row, double roll)
        {
            switch (row.Reward)
            {
                case LootReward.Fragments:
                {
                    // Anyone of that rarity in the bag: a Fragment of a held hero climbs the stars, of an open one
                    // toward a recruit.
                    var pool = row.Rarity is { } r && Get(banner).Weight(r) > 0 ? BagHeroes(r) : BannerHeroes(banner);
                    if (pool.Count == 0) return null;
                    var heroId = pool[(int)Math.Floor(roll * pool.Count)];
                    _heroes.AddFragments(heroId, row.Amount);
                    return new CallLoot(LootKind.Fragments, heroId, row.Amount);
                }
                case LootReward.Stardust:
                {
                    var amount = CallStardust(row.Amount);
                    _treasury.Add(Heroes.STARDUST, amount);
                    return new CallLoot(LootKind.Currency, Heroes.STARDUST, amount);
                }
                case LootReward.HeroXp:
                    return new CallLoot(LootKind.Currency, Heroes.HERO_XP, (int)_heroes.AddXp(row.Amount));
                default:
                    if (string.IsNullOrEmpty(row.Item)) return null;
                    _items.Grant(row.Item, row.Amount);
                    return new CallLoot(LootKind.Item, row.Item, row.Amount);
            }
        }

        // ---- helpers

        private IEnumerable<IHeroDefinition> OfRarity(HeroRarity rarity) => _heroes.All.Where(h => h.Rarity == rarity);

        private static long DayIndex(double now) => (long)Math.Floor(now / DAY_MS);

        // The ledger rolled onto today: lazy, so nothing has to happen at midnight.
        private FreePulls RollFreePulls(string banner, double now)
        {
            var today = DayIndex(now);
            if (_state.FreePulls.TryGetValue(banner, out var held) && held.Day == today) return held;
            var fresh = new FreePulls { Day = today, Used = 0, ReadyAt = held?.ReadyAt ?? 0 };
            _state.FreePulls[banner] = fresh;
            return fresh;
        }
    }
}
