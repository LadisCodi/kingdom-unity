using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Modifiers;
using Codigames.Kingdom.Army;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Battles;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Research;

namespace Codigames.Kingdom.Lairs
{
    public enum LairBlock
    {
        None,
        LairNotFound,
        AlreadyCleared,
        // Beaten, its reward waiting: nobody left to fight.
        AlreadyDefeated,
        EmptyParty,
        // A lair wants soldiers: never a hero alone.
        NoSoldiers,
        TooManySlots,
        NotEnoughUnits,
        NotEnoughSupplies,
        // A hero the kingdom does not own.
        NoHero,
        TooManyHeroes,
        // A hero the last fight left with nothing rests until its HP is full.
        HeroDown,
    }

    public enum LairResult
    {
        // Refused before anyone drew a weapon: the block says why.
        Blocked,
        // A fight on the path fell and the lair still stands.
        Won,
        // The last one fell: the lair waits for its claim.
        Cleared,
        Repelled,
    }

    // One attempt on a lair: what it was, what it cost, and the fight to replay.
    public sealed class LairReport
    {
        public LairResult Result;
        public LairBlock Block;
        // The party's power estimate and the garrison's: neither decided anything, the log did.
        public int Attack;
        public int Power;
        public BattleLog Log;
        public Board Ours;
        public Board Theirs;
        // What the claim will pay, on a clear.
        public IReadOnlyDictionary<string, double> Hoard = new Dictionary<string, double>();
        public double Knowledge;
        // Hero XP a fight short of the last paid on the spot.
        public double HeroXp;
        public IReadOnlyDictionary<string, double> Supplies = new Dictionary<string, double>();
        // Who did not come back, and how many of them reached a bed.
        public IReadOnlyDictionary<string, int> Losses = new Dictionary<string, int>();
        public int Wounded;
    }

    public sealed class LairClaim
    {
        public IReadOnlyDictionary<string, double> Hoard;
        public double HeroXp;
        public double Knowledge;
        public IReadOnlyDictionary<string, int> Items;
        public IReadOnlyList<Relics.FragmentDrop> Fragments = Array.Empty<Relics.FragmentDrop>();
    }

    // Lairs: the party and the fight (Docs/features/18-garrisons-and-raids.md §5). A lair is a path of fights resolved on
    // entry — nothing is ever in flight. Every attack charges its Mana on the way in, win or lose; the garrison swings back
    // either way, and who fell is read straight off the fight. The last fight beats the lair, and its card's Claim pays.
    public class LairAttack
    {
        private const string MANA = "Mana";
        private const string HERO_XP = "HeroXp";
        private const string KNOWLEDGE = "Knowledge";
        private const string UNIT_ATK = "unitAtk";
        private const string UNIT_DEF = "unitDef";
        private const string UNIT_HP = "unitHp";
        private static readonly string[] TAGS = { "Melee", "Distance", "Mounted" };

        private readonly Lairs _lairs;
        private readonly Combat _combat;
        private readonly EnemyGenerator _generator;
        private readonly Army.Army _army;
        private readonly ITreasury _treasury;
        private readonly IItemGrants _items;
        private readonly IBonuses _bonuses;
        private readonly Heroes.Heroes _heroes;
        private IModifiers Stack { get; }
        private readonly uint _seed;
        private readonly int _troopSlots;

        // Where a claim's relic fragments come from; null where relics are not in play.
        private readonly Relics.IRelicDrops _relicDrops;

        public LairAttack(Lairs lairs, Combat combat, EnemyGenerator generator, Army.Army army, ITreasury treasury, uint seed, int troopSlots = 6,
            IItemGrants items = null, IBonuses bonuses = null, Heroes.Heroes heroes = null, IModifiers modifiers = null,
            Relics.IRelicDrops relicDrops = null)
        {
            _relicDrops = relicDrops;
            Stack = modifiers;
            _heroes = heroes;
            _lairs = lairs;
            _combat = combat;
            _generator = generator;
            _army = army;
            _treasury = treasury;
            _seed = seed;
            _troopSlots = troopSlots;
            _items = items;
            _bonuses = bonuses;
        }

        // A lair's attack has seen the end of a fight: the screen, the card and the notices read it.
        public event Action<string, LairReport> Attacked;

        public event Action<string, LairClaim> Claimed;

        public int TroopSlots => _troopSlots;

        public int TickMs => _combat.Settings.TickMs;

        // What is standing in the doorway of fight `index` (the next one by default): derived from the lair's guard, the
        // last fight rolled under the key the lair's one fight always had.
        public Board Board(ILairSite lair, int? index = null)
        {
            var plan = Plan(lair, index);
            return _combat.BuildBoard(plan.Squads, plan.Fighters);
        }

        public IReadOnlyList<SquadSpec> Formation(ILairSite lair) => Plan(lair, null).Squads;

        public int Power(ILairSite lair) => Combat.BoardPower(Board(lair));


        // Our side of the board: the squads sent with the kingdom's drill, and the heroes with what they have left.
        public Board PartyBoard(IEnumerable<SquadSpec> slots, IReadOnlyList<string> heroes = null, double now = 0)
            => _combat.BuildBoard(slots.Where(s => s.Count > 0).ToList(), Fighters(heroes, now), Drill());

        private IReadOnlyList<FighterSpec> Fighters(IReadOnlyList<string> heroes, double now)
            => heroes == null || _heroes == null ? Array.Empty<FighterSpec>()
                : heroes.Select(h => _heroes.Fighter(h, now, _combat.Settings.TickMs, _combat.Settings.HeroPowerPerDmg)).ToList();

        // The party's power: an estimate, never the outcome.
        public int PartyPower(IEnumerable<SquadSpec> slots, IReadOnlyList<string> heroes = null)
            => Combat.JsRound(slots.Where(s => s.Count > 0).Sum(s => (double)_combat.RankOf(s.Troop).Power * s.Count)
                              + (heroes == null || _heroes == null ? 0 : heroes.Sum(h => _heroes.Power(h, _combat.Settings.HeroPowerPerDmg))));

        public IReadOnlyDictionary<string, double> Supplies => _combat.Settings.FightMana > 0
            ? new Dictionary<string, double> { [MANA] = _combat.Settings.FightMana }
            : new Dictionary<string, double>();

        public LairBlock Block(string id, IReadOnlyList<SquadSpec> slots, IReadOnlyList<string> heroes = null, double now = 0)
        {
            heroes ??= Array.Empty<string>();
            var state = _lairs.StateOf(id);
            if (state == null) return LairBlock.LairNotFound;
            if (state.Cleared) return LairBlock.AlreadyCleared;
            if (state.Defeated) return LairBlock.AlreadyDefeated;
            if (heroes.Any(h => _heroes == null || !_heroes.Owns(h))) return LairBlock.NoHero;
            if (heroes.Count > (_heroes?.Slots ?? 0)) return LairBlock.TooManyHeroes;
            if (heroes.Any(h => !_heroes.CanFight(h, now))) return LairBlock.HeroDown;
            var committed = slots.Where(s => s.Count > 0).ToList();
            // A lair wants soldiers: soldiers alone, or with heroes — never a hero alone.
            if (heroes.Count == 0 && committed.Count == 0) return LairBlock.EmptyParty;
            if (committed.Count == 0) return LairBlock.NoSoldiers;
            if (committed.Count > _troopSlots) return LairBlock.TooManySlots;
            foreach (var group in committed.GroupBy(s => s.Troop))
                if (group.Sum(s => s.Count) > _army.Count(group.Key)) return LairBlock.NotEnoughUnits;
            return _treasury.CanAfford(Supplies) ? LairBlock.None : LairBlock.NotEnoughSupplies;
        }

        public LairReport Attack(string id, IReadOnlyList<SquadSpec> slots, double now, IReadOnlyList<string> heroes = null)
        {
            heroes ??= Array.Empty<string>();
            var lair = _lairs.Site(id);
            var theirs = lair == null ? null : Board(lair);
            var report = new LairReport { Power = theirs == null ? 0 : Combat.BoardPower(theirs), Supplies = Supplies, Theirs = theirs };
            report.Block = Block(id, slots, heroes, now);
            if (report.Block != LairBlock.None)
            {
                report.Result = LairResult.Blocked;
                return report;
            }

            _treasury.TryPay(Supplies);
            var committed = slots.Where(s => s.Count > 0).ToList();
            var ours = PartyBoard(committed, heroes, now);
            report.Ours = ours;
            report.Attack = PartyPower(committed, heroes);
            report.Log = _combat.Resolve(ours, theirs);

            // Win or lose, every hero keeps what the fight did to it.
            var pools = Combat.PoolsAfter(report.Log, Side.Ours);
            foreach (var slot in ours.Slots.Where(s => s.IsHero))
                _heroes?.SetHp(slot.FighterId, (int)Math.Floor(pools.TryGetValue(slot.Id, out var hp) ? hp : slot.HpPool), now);
            var spoils = Spoils(ours);

            // Who fell, read off the fight: a share carried to the Infirmary's beds, the rest gone.
            var left = Combat.Survivors(report.Log, Side.Ours);
            var losses = new Dictionary<string, int>();
            foreach (var slot in ours.Slots.Where(s => !s.IsHero))
            {
                var fell = slot.Count - (left.TryGetValue(slot.Id, out var alive) ? alive : slot.Count);
                if (fell > 0) losses[slot.Troop] = (losses.TryGetValue(slot.Troop, out var n) ? n : 0) + fell;
            }

            report.Losses = losses;
            // A Field medic carries its points of the fallen home; someone always stays out there.
            report.Wounded = losses.Count > 0 ? _army.Lose(losses, Math.Min(WOUNDED_SHARE_CAP, Math.Max(0, _army.WoundedShare + spoils.Medic))) : 0;

            if (report.Log.Winner != Side.Ours)
            {
                report.Result = LairResult.Repelled;
                Attacked?.Invoke(id, report);
                return report;
            }

            // A fight short of the last pays its share of the lair's Hero XP now, so none is fought for nothing.
            if (!_lairs.WinFight(id))
            {
                report.HeroXp = PayHeroXp(Prices.RoundPrice(_lairs.FightXp(lair) * (1 + spoils.Seasoned)));
                report.Result = LairResult.Won;
                Attacked?.Invoke(id, report);
                return report;
            }

            // The last fight: Plunder swells the hoard now; Lore and Seasoned ride on the claim.
            var beaten = _lairs.StateOf(id);
            if (spoils.Plunder > 0)
                foreach (var currency in beaten.Hoard.Keys.ToList())
                    beaten.Hoard[currency] = Math.Round(beaten.Hoard[currency] * (1 + spoils.Plunder), MidpointRounding.AwayFromZero);
            beaten.SpoilsLore = spoils.Lore;
            beaten.SpoilsSeasoned = spoils.Seasoned;
            report.Hoard = new Dictionary<string, double>(beaten.Hoard);
            report.Knowledge = _lairs.ClearReward(lair).Knowledge;
            report.Result = LairResult.Cleared;
            Attacked?.Invoke(id, report);
            return report;
        }

        // The claim: the hoard comes home, the last fight's Hero XP and the first-clear Knowledge are paid, the tier's
        // items go into the Bag, and the lair is gone. Null when it is not beaten or already claimed.
        public LairClaim Claim(string id)
        {
            var lair = _lairs.Site(id);
            if (lair == null || !_lairs.AwaitsClaim(id)) return null;
            var (heroXp, knowledge) = _lairs.ClearReward(lair);
            var claim = new LairClaim { Hoard = _lairs.Claim(id, _treasury), Knowledge = knowledge, Items = _lairs.GarrisonOf(lair).RewardItems };
            claim.HeroXp = PayHeroXp(heroXp);
            if (knowledge > 0) _treasury.Add(KNOWLEDGE, knowledge);
            if (_items != null)
                foreach (var (item, count) in claim.Items)
                    _items.Grant(item, count);
            // A lair is a city relic's door.
            if (_relicDrops != null) claim.Fragments = _relicDrops.ForLair(id, lair.Tier);
            Claimed?.Invoke(id, claim);
            return claim;
        }

        private const double WOUNDED_SHARE_CAP = 0.9;

        // Hero XP through the heroes' own counter — the tree, the Taverns, the boons — or, with no heroes, the tree alone.
        private double PayHeroXp(double amount)
        {
            if (_heroes != null) return _heroes.AddXp(amount);
            var paid = Math.Round(_bonuses.Apply("heroXp", amount), MidpointRounding.AwayFromZero);
            if (paid > 0) _treasury.Add(HERO_XP, paid);
            return paid;
        }

        // What a won fight pays on top, from the spoils skills on our side: shares, summed across heroes.
        private static (double Plunder, double Lore, double Seasoned, double Medic) Spoils(Board board)
        {
            double plunder = 0, lore = 0, seasoned = 0, medic = 0;
            foreach (var slot in board.Slots.Where(s => s.IsHero && s.Skill != null))
            {
                var share = slot.Skill.Amount / 1000.0;
                switch (slot.Skill.Id)
                {
                    case "Plunder": plunder += share; break;
                    case "Lore": lore += share; break;
                    case "Seasoned": seasoned += share; break;
                    case "FieldMedic": medic += share; break;
                }
            }

            return (plunder, lore, seasoned, medic);
        }

        private EnemyPlan Plan(ILairSite lair, int? index)
        {
            var i = index ?? _lairs.FightIndex(lair);
            var last = i >= _lairs.Fights(lair) - 1;
            var parts = last ? new object[] { lair.RollKey, "gate" } : new object[] { lair.RollKey, "gate", i };
            return _generator.Generate(_seed, parts, _lairs.FightPower(lair, i), lair.Threat, lair.Mix.Count > 0 ? lair.Mix : null);
        }

        // The kingdom's drill: the tree's share on a unit's own Attack (which moves its damage) and Defence — unaimed plus
        // every tag it carries — and its health, never below the sheet's.
        private TroopBonus Drill()
        {
            double Pct(string stat, string troop)
            {
                var all = _bonuses?.Totals(stat).Percent ?? 0;
                var tags = _combat.Unit(Troops.UnitOf(troop)).Tags;
                return all + TAGS.Where(tags.Contains).Sum(tag => Aimed(stat, tag));
            }

            return new TroopBonus(
                troop => _combat.RankOf(troop).Dmg * Pct(UNIT_ATK, troop),
                troop => _combat.RankOf(troop).Def * Pct(UNIT_DEF, troop),
                _ => Math.Max(1, Stack.Apply(UNIT_HP, _bonuses.Multiplier(UNIT_HP))));
        }

        // What is aimed at exactly this tag, without the unaimed share.
        private double Aimed(string stat, string tag)
            => _bonuses == null ? 0 : _bonuses.Totals(stat, TargetKind.UnitTag, tag).Percent - _bonuses.Totals(stat).Percent;
    }
}
