namespace Kingdom.Sim.Data
{
    // A relic: one effect whose number rises with its level.
    public sealed class ArtifactDef
    {
        public string Id { get; set; }
        // city | world.
        public string Kind { get; set; }
        // Where its first fragment is found.
        public string Door { get; set; }
        public RelicPassive Passive { get; set; }
        // Null on every city relic and on a world relic without a spell.
        public RelicActive Active { get; set; }
        // Null on a world relic.
        public RelicActivation Activation { get; set; }
    }
}
