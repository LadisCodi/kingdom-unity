namespace Kingdom.Sim.Data
{
    // An enemy hero: the same shape, its numbers authored per room.
    public sealed class VillainDef
    {
        public string Id { get; set; }
        public VillainData Data { get; set; }
        public SkillDef Skill { get; set; }
    }
}
