namespace Kingdom.Sim.State
{
    // A build under way on a held hex.
    public sealed class WorldBuild
    {
        public double Index { get; set; }
        public string What { get; set; }
        public double Level { get; set; }
        public double FinishesAt { get; set; }
    }
}
