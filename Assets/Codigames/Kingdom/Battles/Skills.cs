using System.Collections.Generic;

namespace Codigames.Kingdom.Battles
{
    public enum SkillKind
    {
        Strike,
        Heal,
        Shield,
        Daze,
        Rally,
        Spoils,
    }

    // What each skill does, by id (§9.3): a timed one fires on its own clock, a rally is baked into the board, a spoil
    // is the caller's.
    public static class Skills
    {
        private static readonly IReadOnlyDictionary<string, SkillKind> KINDS = new Dictionary<string, SkillKind>
        {
            ["Sharpshot"] = SkillKind.Strike, ["Crush"] = SkillKind.Strike, ["Cleave"] = SkillKind.Strike, ["Ambush"] = SkillKind.Strike,
            ["Volley"] = SkillKind.Strike, ["Mend"] = SkillKind.Heal, ["Wave"] = SkillKind.Heal, ["Shield"] = SkillKind.Shield,
            ["Daze"] = SkillKind.Daze, ["WarCry"] = SkillKind.Rally, ["Bulwark"] = SkillKind.Rally, ["Vigour"] = SkillKind.Rally,
            ["Plunder"] = SkillKind.Spoils, ["Lore"] = SkillKind.Spoils, ["Seasoned"] = SkillKind.Spoils, ["FieldMedic"] = SkillKind.Spoils,
        };

        public static SkillKind KindOf(string id) => KINDS.TryGetValue(id, out var kind) ? kind : SkillKind.Spoils;

        public static bool IsTimed(string id) => KindOf(id) is SkillKind.Strike or SkillKind.Heal or SkillKind.Shield or SkillKind.Daze;
    }
}
