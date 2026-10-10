using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Battles;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Goods;
using Codigames.Kingdom.Heroes.State;
using Codigames.Kingdom.Modifiers;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Heroes
{
    public enum HeroLevelResult
    {
        Levelled,
        NotOwned,
        AtMaxLevel,
        AscensionCapped,
        NotEnoughXp,
    }

    public enum HeroAscendResult
    {
        Ascended,
        NotOwned,
        AtMaxAscension,
        NotEnoughFragments,
        NotEnoughStardust,
    }

    public enum HeroRecruitResult
    {
        Recruited,
        AlreadyOwned,
        NotEnoughFragments,
    }

    public enum SkillRankBlock
    {
        None,
        NotOwned,
        AtMaxRank,
        LevelTooLow,
        NotEnoughStardust,
        NotEnoughMaterial,
    }

    public enum HeroSlotResult
    {
        Purchased,
        AtMax,
        NotEnoughGems,
    }

    // The kingdom's heroes (Docs/features/10-heroes.md): the collection — recruited from fragments or handed over by a
    // call — and each hero's ladder: levels on Hero XP inside the cap its ascension sets, ascension points on its own
    // fragments and a Stardust toll, skill ranks on Stardust and its family's material. Wounds carry between fights and
    // mend on their own. A Legendary owned is a boon on the kingdom. One hero slot is free, the rest are Gems.
    public class Heroes : IModifierSource
    {
        public const string HERO_XP = "HeroXp";
        public const string STARDUST = "Stardust";
        private const string GEMS = "Gems";
        private const double HOUR_MS = 3_600_000;
        private const string BOON = "hero:";

        // The family's material a rank asks for (Docs/features/10-heroes.md §2.5.1).
        private static readonly IReadOnlyDictionary<SkillKind, string> MATERIAL = new Dictionary<SkillKind, string>
        {
            [SkillKind.Strike] = "Starmetal", [SkillKind.Heal] = "Moonglass", [SkillKind.Shield] = "Moonglass",
            [SkillKind.Rally] = "Heartwood", [SkillKind.Daze] = "Heartwood", [SkillKind.Spoils] = "Heartwood",
        };

        private readonly HeroesState _state;
        private readonly ICatalog<IHeroDefinition> _heroes;
        private readonly HeroLadder _ladder;
        private readonly ITreasury _treasury;
        private readonly Stockpile _stockpile;
        private readonly CityState _city;
        private readonly ICatalog<City.IBuildingDefinition> _buildings;
        private readonly IBonuses _bonuses;
        private readonly Func<IModifiers> _modifiers;

        public Heroes(HeroesState state, ICatalog<IHeroDefinition> heroes, HeroLadder ladder, ITreasury treasury, Stockpile stockpile = null,
            CityState city = null, ICatalog<City.IBuildingDefinition> buildings = null, IBonuses bonuses = null, Func<IModifiers> modifiers = null)
        {
            _state = state;
            _heroes = heroes;
            _ladder = ladder;
            _treasury = treasury;
            _stockpile = stockpile;
            _city = city;
            _buildings = buildings;
            _bonuses = bonuses;
            _modifiers = modifiers;
        }

        // Anything about a hero moved: a recruit, a level, a star, a rank, a wound, a slot.
        public event Action Changed;

        public HeroLadder Ladder => _ladder;
        public IReadOnlyList<string> Owned => _state.Owned;
        public IReadOnlyList<IHeroDefinition> All => _heroes.Items;
        public IHeroDefinition Get(string id) => _heroes.Get(id);
        public bool Owns(string id) => _state.Owned.Contains(id);
        public int Level(string id) => _state.Levels.TryGetValue(id, out var n) ? n : 1;
        public int Ascension(string id) => _state.Ascension.TryGetValue(id, out var n) ? n : 0;
        public int Fragments(string id) => _state.Fragments.TryGetValue(id, out var n) ? n : 0;

        // A hero joins at level 1 with every star empty; one already owned turns into fragments.
        public bool Grant(string id, int duplicateFragments = 0)
        {
            if (Owns(id))
            {
                _state.Fragments[id] = Fragments(id) + duplicateFragments;
                Changed?.Invoke();
                return false;
            }

            _state.Owned.Add(id);
            _state.Levels[id] = 1;
            _state.Ascension[id] = 0;
            _state.Fragments[id] = Fragments(id);
            Changed?.Invoke();
            return true;
        }

        public void AddFragments(string id, int count)
        {
            _state.Fragments[id] = Fragments(id) + count;
            Changed?.Invoke();
        }

        // ---- the ladder

        public int LevelCap(string id) => _ladder.LevelCap(Ascension(id));

        public double LevelCost(string id) => _ladder.XpLevelCost(Level(id));

        public HeroLevelResult LevelUp(string id)
        {
            if (!Owns(id)) return HeroLevelResult.NotOwned;
            var level = Level(id);
            if (level >= _ladder.Settings.HeroMaxLevel) return HeroLevelResult.AtMaxLevel;
            if (level >= LevelCap(id)) return HeroLevelResult.AscensionCapped;
            var cost = _ladder.XpLevelCost(level);
            if (!_treasury.TryPay(new Dictionary<string, double> { [HERO_XP] = cost })) return HeroLevelResult.NotEnoughXp;
            _state.Levels[id] = level + 1;
            Changed?.Invoke();
            return HeroLevelResult.Levelled;
        }

        public int RecruitCost(string id) => _ladder.Settings.RecruitFragments(_heroes.Get(id).Rarity);

        public bool CanRecruit(string id) => !Owns(id) && Fragments(id) >= RecruitCost(id);

        // Fragments that buy a hero outright — not an ascension: it starts with every star empty, change carried over.
        public HeroRecruitResult Recruit(string id)
        {
            if (Owns(id)) return HeroRecruitResult.AlreadyOwned;
            if (Fragments(id) < RecruitCost(id)) return HeroRecruitResult.NotEnoughFragments;
            _state.Fragments[id] = Fragments(id) - RecruitCost(id);
            Grant(id);
            return HeroRecruitResult.Recruited;
        }

        public double AscensionFragmentCost(string id) => _ladder.AscensionFragmentCost(Ascension(id));

        public double AscensionStardustCost(string id) => _ladder.AscensionStardustCost(Ascension(id));

        // The next point of the current star: both prices, or neither.
        public HeroAscendResult Ascend(string id)
        {
            if (!Owns(id)) return HeroAscendResult.NotOwned;
            var ascension = Ascension(id);
            if (ascension >= _ladder.MaxAscension) return HeroAscendResult.AtMaxAscension;
            var fragments = _ladder.AscensionFragmentCost(ascension);
            if (Fragments(id) < fragments) return HeroAscendResult.NotEnoughFragments;
            if (!_treasury.TryPay(new Dictionary<string, double> { [STARDUST] = _ladder.AscensionStardustCost(ascension) }))
                return HeroAscendResult.NotEnoughStardust;
            _state.Fragments[id] = Fragments(id) - (int)fragments;
            _state.Ascension[id] = ascension + 1;
            Changed?.Invoke();
            return HeroAscendResult.Ascended;
        }

        public (double Atk, double Dmg, double Def, double Hp) Body(string id) => _ladder.Body(_heroes.Get(id), Level(id), Ascension(id));

        // ---- skill ranks

        public int SkillRank(string id) => Math.Min(_ladder.MaxSkillRank, _state.SkillRanks.TryGetValue(id, out var r) ? r : 1);

        // The level the next rank unlocks at; null at the top.
        public int? NextSkillRankLevel(string id)
        {
            var rank = SkillRank(id);
            return rank - 1 < _ladder.Settings.SkillRankLevels.Count ? _ladder.Settings.SkillRankLevels[rank - 1] : null;
        }

        // What the next rank costs: Stardust, and the family's material while precious materials are in play.
        public (double Stardust, IReadOnlyDictionary<string, double> Goods)? SkillRankPrice(string id)
        {
            var rank = SkillRank(id);
            if (rank >= _ladder.MaxSkillRank) return null;
            var material = MATERIAL[Skills.KindOf(_heroes.Get(id).Skill)];
            var asked = new Dictionary<string, double> { [material] = _ladder.Settings.SkillRankMaterial[rank - 1] };
            return (_ladder.Settings.SkillRankStardust[rank - 1], _stockpile?.Priced(asked) ?? new Dictionary<string, double>());
        }

        public SkillRankBlock SkillRankRefusal(string id)
        {
            if (!Owns(id)) return SkillRankBlock.NotOwned;
            if (SkillRankPrice(id) is not { } price) return SkillRankBlock.AtMaxRank;
            if (Level(id) < NextSkillRankLevel(id)) return SkillRankBlock.LevelTooLow;
            if (!_treasury.CanAfford(new Dictionary<string, double> { [STARDUST] = price.Stardust })) return SkillRankBlock.NotEnoughStardust;
            if (_stockpile != null && !_stockpile.CanAfford(price.Goods)) return SkillRankBlock.NotEnoughMaterial;
            return SkillRankBlock.None;
        }

        // Never raised on its own: a level only unlocks the purchase.
        public SkillRankBlock BuySkillRank(string id)
        {
            var block = SkillRankRefusal(id);
            if (block != SkillRankBlock.None) return block;
            var price = SkillRankPrice(id).Value;
            _treasury.TryPay(new Dictionary<string, double> { [STARDUST] = price.Stardust });
            _stockpile?.TryPay(price.Goods);
            _state.SkillRanks[id] = SkillRank(id) + 1;
            Changed?.Invoke();
            return SkillRankBlock.None;
        }

        // ---- slots

        public int Slots => Math.Min(_ladder.Settings.HeroSlots, 1 + _state.SlotsPurchased);

        public double SlotGemCost => Prices.RoundPrice(_ladder.Settings.HeroSlotGemCostBase * Math.Pow(_ladder.Settings.HeroSlotGemCostGrowth, _state.SlotsPurchased));

        // Slots handed over for good (a product), up to the ceiling as the count reads them.
        public void GrantSlots(int count)
        {
            if (count <= 0) return;
            _state.SlotsPurchased += count;
            Changed?.Invoke();
        }

        public HeroSlotResult BuySlot()
        {
            if (Slots >= _ladder.Settings.HeroSlots) return HeroSlotResult.AtMax;
            if (!_treasury.TryPay(new Dictionary<string, double> { [GEMS] = SlotGemCost })) return HeroSlotResult.NotEnoughGems;
            _state.SlotsPurchased++;
            Changed?.Invoke();
            return HeroSlotResult.Purchased;
        }

        // ---- Hero XP

        // What a fight taught: one kingdom counter, raised by the tree, every Tavern level and the boons; whole points.
        public double AddXp(double amount)
        {
            var paid = Math.Round(_modifiers?.Invoke().Apply("heroXp", _bonuses.Apply("heroXp", amount) * TavernMultiplier) ?? _bonuses.Apply("heroXp", amount) * TavernMultiplier,
                MidpointRounding.AwayFromZero);
            if (paid > 0) _treasury.Add(HERO_XP, paid);
            return paid;
        }

        // Every standing Tavern's percent at its level, summed: ×1 with none.
        public double TavernMultiplier
        {
            get
            {
                if (_city == null || _buildings == null) return 1;
                var pct = 0.0;
                foreach (var d in _city.Districts.Where(d => d.Built))
                {
                    var list = _buildings.Get(d.DefinitionId).Production.HeroXpBonusPerLevel;
                    if (list.Count > 0) pct += list[Math.Min(d.Level, list.Count) - 1];
                }

                return 1 + pct / 100;
            }
        }

        // ---- wounds (Docs/features/10-heroes.md §2.8)

        public int MaxHp(string id) => Combat.JsRound(Body(id).Hp);

        // A share of the bar, mending linearly: a whole bar every recover hours.
        public double HpShare(string id, double now)
        {
            if (!_state.Hurt.TryGetValue(id, out var hurt)) return 1;
            var missing = Math.Max(0, hurt.Missing - Math.Max(0, now - hurt.At) / (_ladder.Settings.HeroRecoverHours * HOUR_MS));
            return 1 - Math.Min(1, missing);
        }

        public int Hp(string id, double now) => (int)Math.Floor(MaxHp(id) * HpShare(id, now) + 1e-9);

        // Taken to 0, it rests until the bar is whole again.
        public bool Exhausted(string id, double now) => _state.Hurt.TryGetValue(id, out var hurt) && hurt.Exhausted && HpShare(id, now) < 1;

        public double? RestEndsAt(string id, double now)
            => Exhausted(id, now) ? now + Math.Ceiling((1 - HpShare(id, now)) * _ladder.Settings.HeroRecoverHours * HOUR_MS) : null;

        public bool CanFight(string id, double now) => Owns(id) && !Exhausted(id, now) && Hp(id, now) > 0;

        // What a fight left: kept as a share, so a level gained while hurt keeps the same share missing.
        public void SetHp(string id, int hp, double now)
        {
            var max = MaxHp(id);
            var missing = max <= 0 ? 0 : 1 - Math.Max(0, Math.Min(max, hp)) / (double)max;
            if (missing <= 0) _state.Hurt.Remove(id);
            else _state.Hurt[id] = new HeroWound { Missing = missing, At = now, Exhausted = hp <= 0 };
            Changed?.Invoke();
        }

        // ---- on the board

        // The power a hero adds to the estimate.
        public double Power(string id, double heroPowerPerDmg) => Body(id).Dmg * heroPowerPerDmg;

        // A hero as the resolver reads it, with the HP it walks in with.
        public FighterSpec Fighter(string id, double now, int tickMs, double heroPowerPerDmg)
        {
            var hero = _heroes.Get(id);
            var body = Body(id);
            return new FighterSpec
            {
                // Its level's numbers unrounded, as the board holds them; only Attack is a whole rating there.
                Id = id, Name = hero.Name, Type = hero.UnitType, Atk = body.Atk, Dmg = body.Dmg, Def = body.Def,
                Hp = body.Hp, HpNow = Hp(id, now), Cooldown = hero.Cooldown, Power = Combat.JsRound(body.Dmg * heroPowerPerDmg),
                TroopDmgMult = hero.TroopDmgMult, TroopHpMult = hero.TroopHpMult, TroopDefBonus = hero.TroopDefBonus,
                Skill = _ladder.SlotSkill(hero, SkillRank(id), tickMs),
            };
        }

        // ---- the boons: a kingdom multiplier for every Legendary owned, summed across heroes

        // Folded once per hero called (a hero is never lost), since the stack asks it every frame.
        public IEnumerable<Modifier> Modifiers
        {
            get
            {
                if (_boonsOf != _state.Owned || _boonsCount != _state.Owned.Count)
                {
                    _boons = _state.Owned.Select(_heroes.Get).Where(h => h.BoonStat != null)
                        .Select(h => new Modifier(BOON + h.Id, h.BoonStat, ModifierOp.Mul, h.BoonValue)).ToList();
                    _boonsOf = _state.Owned;
                    _boonsCount = _state.Owned.Count;
                }

                return _boons;
            }
        }

        private List<Modifier> _boons;
        private List<string> _boonsOf;
        private int _boonsCount = -1;
    }
}
