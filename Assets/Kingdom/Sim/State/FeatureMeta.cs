namespace Kingdom.Sim.State
{
    // Where a feature came from, and how many times it respawned.
    public sealed class FeatureMeta
    {
        public string Origin { get; set; }
        public double Generation { get; set; }
    }
}
