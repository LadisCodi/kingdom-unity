namespace Codigames.Kingdom.Data
{
    // A hero: its rarity, unit type, skill, body and passive.
    public sealed class HeroDef
    {
        public string Id { get; set; }
        public HeroData Data { get; set; }
        public SkillDef Skill { get; set; }
    }
}
