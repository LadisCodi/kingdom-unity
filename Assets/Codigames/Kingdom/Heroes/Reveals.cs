using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Bag;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Heroes
{
    public enum PrizeKind
    {
        Hero,
        Fragments,
        Currency,
        Item,
        // A ten-call's supplies of one family — every speed-up, or every chest — as one card.
        Supplies,
        // A ten-call's Fragments, every hero that got any on one card.
        Bag,
    }

    public enum SupplyFamily
    {
        Speedup,
        Chest,
    }

    // What a reveal is opened from: the silver chest of the common call, the gold one of the golden call, the relic
    // chest of a fragment pack, the war chest of a fight's spoils.
    public enum RevealChest
    {
        Common,
        Golden,
        Relic,
        Spoils,
    }

    // Where a call's fragments of one hero leave them: toward recruiting a stranger, or toward the next ascension point
    // of one owned. From and To are the fragments held before and after; Recruited, that the call filled the bar.
    public sealed class FragmentProgress
    {
        public bool TowardRecruit { get; set; }
        public int From { get; set; }
        public int To { get; set; }
        public int Goal { get; set; }
        public bool Recruited { get; set; }
    }

    public sealed class BagRow
    {
        public string HeroId { get; set; }
        public int Amount { get; set; }
        public FragmentProgress Progress { get; set; }
    }

    // One prize as the reveal deals it: a hero; a hero's fragments; a currency or an item and how many; a family of
    // supplies with what is in it; the bag of every fragment.
    public sealed class Prize
    {
        public PrizeKind Kind { get; set; }
        // The hero, the currency or the item.
        public string Id { get; set; }
        public int Amount { get; set; }
        public FragmentProgress Progress { get; set; }
        public SupplyFamily Family { get; set; }
        public IReadOnlyList<(string Item, int Amount)> Items { get; set; } = new List<(string, int)>();
        public IReadOnlyList<BagRow> Rows { get; set; } = new List<BagRow>();

        public static Prize Currency(string currency, int amount) => new() { Kind = PrizeKind.Currency, Id = currency, Amount = amount };
    }

    // A chest of prizes for the reveal (Docs/features/10-heroes.md §8.3): the chest it opens from, how many calls it
    // was or the line it carries instead, and its prizes in the order they are dealt.
    public sealed class Reveal
    {
        public RevealChest Chest { get; set; }
        public int Calls { get; set; }
        public string Caption { get; set; }
        public IReadOnlyList<Prize> Prizes { get; set; } = new List<Prize>();
    }

    // A call's record of rolls turned into what the player got (the web's gachaPrizes, groupPrizes and openReveal):
    // the same thing summed into one prize, heroes last; fragments that reach the recruiting price recruit on the spot,
    // since the reveal is where the player watches the bar fill; a ten-call grouped into a handful of cards.
    public class Reveals
    {
        private readonly Heroes _heroes;
        private readonly ICatalog<IItemDefinition> _items;

        public Reveals(Heroes heroes, ICatalog<IItemDefinition> items)
        {
            _heroes = heroes;
            _items = items;
        }

        public Reveal Open(string banner, IReadOnlyList<PullResult> pulls)
        {
            var prizes = FromPulls(pulls);
            foreach (var p in prizes.Where(p => p.Kind == PrizeKind.Fragments))
            {
                var held = _heroes.Fragments(p.Id);
                if (!_heroes.Owns(p.Id))
                {
                    var recruited = _heroes.Recruit(p.Id) == HeroRecruitResult.Recruited;
                    p.Progress = new FragmentProgress
                    {
                        TowardRecruit = true, From = held - p.Amount, To = held, Goal = _heroes.RecruitCost(p.Id), Recruited = recruited,
                    };
                }
                else if (_heroes.Ascension(p.Id) < _heroes.Ladder.MaxAscension)
                {
                    p.Progress = new FragmentProgress
                    {
                        From = held - p.Amount, To = held, Goal = (int)_heroes.AscensionFragmentCost(p.Id),
                    };
                }
            }

            // A recruit is what the call was for: after the other fragments, just before the heroes.
            int Rank(Prize p) => p.Kind == PrizeKind.Hero ? 2 : p.Kind == PrizeKind.Fragments && p.Progress?.Recruited == true ? 1 : 0;
            var ordered = prizes.OrderBy(Rank).ToList();
            return new Reveal
            {
                Chest = banner == Gacha.STANDARD ? RevealChest.Common : RevealChest.Golden,
                Calls = pulls.Count,
                Prizes = pulls.Count > 1 ? Group(ordered) : ordered,
            };
        }

        // Same thing, one prize with a count; currencies, then items, then fragments, then the heroes. A duplicate is
        // not a hero prize: it already paid its fragments.
        public static List<Prize> FromPulls(IReadOnlyList<PullResult> pulls)
        {
            var heroes = new List<Prize>();
            var fragments = new List<(string Id, int Amount)>();
            var currencies = new List<(string Id, int Amount)>();
            var items = new List<(string Id, int Amount)>();

            void Add(List<(string Id, int Amount)> list, string id, int n)
            {
                var at = list.FindIndex(e => e.Id == id);
                if (at < 0) list.Add((id, n));
                else list[at] = (id, list[at].Amount + n);
            }

            foreach (var p in pulls)
            {
                if (p.HeroId != null && !p.Duplicate) heroes.Add(new Prize { Kind = PrizeKind.Hero, Id = p.HeroId });
                if (p.FragmentsOf != null && p.Fragments > 0) Add(fragments, p.FragmentsOf, p.Fragments);
                foreach (var l in p.Loot)
                {
                    if (l.Kind == LootKind.Fragments) Add(fragments, l.Id, l.Amount);
                    else if (l.Kind == LootKind.Currency) Add(currencies, l.Id, l.Amount);
                    else Add(items, l.Id, l.Amount);
                }
            }

            return currencies.Select(c => Prize.Currency(c.Id, c.Amount))
                .Concat(items.Select(i => new Prize { Kind = PrizeKind.Item, Id = i.Id, Amount = i.Amount }))
                .Concat(fragments.Select(f => new Prize { Kind = PrizeKind.Fragments, Id = f.Id, Amount = f.Amount }))
                .Concat(heroes)
                .ToList();
        }

        // A ten-call as a handful of cards: one per currency, one per supply family with its contents, one bag of every
        // Fragment, and a card per new hero — a recruit on the bag gets one too, so it is celebrated as a hero.
        public List<Prize> Group(IReadOnlyList<Prize> prizes)
        {
            var currencies = new List<Prize>();
            var families = new List<(SupplyFamily Family, List<(string, int)> Items)>();
            var rows = new List<BagRow>();
            var heroes = new List<Prize>();
            var rest = new List<Prize>();
            foreach (var p in prizes)
            {
                if (p.Kind == PrizeKind.Currency) currencies.Add(p);
                else if (p.Kind == PrizeKind.Item && FamilyOf(p.Id) is { } family)
                {
                    var at = families.FindIndex(f => f.Family == family);
                    if (at < 0) families.Add((family, new List<(string, int)> { (p.Id, p.Amount) }));
                    else families[at].Items.Add((p.Id, p.Amount));
                }
                else if (p.Kind == PrizeKind.Fragments)
                {
                    rows.Add(new BagRow { HeroId = p.Id, Amount = p.Amount, Progress = p.Progress });
                    if (p.Progress?.Recruited == true) heroes.Add(new Prize { Kind = PrizeKind.Hero, Id = p.Id });
                }
                else if (p.Kind == PrizeKind.Hero) heroes.Add(p);
                else rest.Add(p);
            }

            var supplies = new[] { SupplyFamily.Speedup, SupplyFamily.Chest }
                .Where(f => families.Any(x => x.Family == f))
                .Select(f =>
                {
                    var list = families.First(x => x.Family == f).Items;
                    return new Prize { Kind = PrizeKind.Supplies, Family = f, Items = list, Amount = list.Sum(i => i.Item2) };
                });
            // The bag's recruits last, so its rows read toward the heroes after it.
            var bag = rows.OrderBy(r => r.Progress?.Recruited == true ? 1 : 0).ToList();
            return currencies.Concat(supplies).Concat(rest)
                .Concat(bag.Count > 0 ? new[] { new Prize { Kind = PrizeKind.Bag, Rows = bag } } : new Prize[0])
                .Concat(heroes)
                .ToList();
        }

        private SupplyFamily? FamilyOf(string item)
            => _items.TryGet(item, out var def) ? def.Kind switch
            {
                ItemKind.Speedup => SupplyFamily.Speedup,
                ItemKind.Chest => SupplyFamily.Chest,
                _ => null,
            } : null;
    }
}
