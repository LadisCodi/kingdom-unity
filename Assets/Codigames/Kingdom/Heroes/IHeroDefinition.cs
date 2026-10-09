using Codigames.Modules.Core;

namespace Codigames.Kingdom.Heroes
{
    public enum HeroRarity
    {
        Common,
        Rare,
        Legendary,
    }

    // A hero (Docs/features/10-heroes.md §2): its rarity and the unit type it fights as, its stat block at level 1 and
    // what each level adds, the passive it gives troops of its type, its one skill, and — a Legendary only — its boon.
    public interface IHeroDefinition : IIdentifiable
    {
        string Name { get; }
        string Title { get; }
        HeroRarity Rarity { get; }
        // Its place in its rarity's bag order, the order new heroes arrive in; null for the rest, shuffled per kingdom.
        int? BagRank { get; }
        string UnitType { get; }
        string Skill { get; }
        // The skill's rank-1 value (a percent, a flat, seconds) and how often it fires, in seconds.
        double SkillValue { get; }
        double SkillEvery { get; }
        double Atk { get; }
        double Dmg { get; }
        double Def { get; }
        double Hp { get; }
        int Cooldown { get; }
        double AtkPerLevel { get; }
        double DmgPerLevel { get; }
        double DefPerLevel { get; }
        double HpPerLevel { get; }
        double TroopDmgMult { get; }
        double TroopHpMult { get; }
        int TroopDefBonus { get; }
        // A kingdom multiplier on while it is owned; null below Legendary.
        string BoonStat { get; }
        double BoonValue { get; }
    }
}
