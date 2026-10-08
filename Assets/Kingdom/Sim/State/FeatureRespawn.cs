namespace Kingdom.Sim.State
{
    // A consumed feature waiting to come back near its origin.
    public sealed class FeatureRespawn
    {
        public string Origin { get; set; }
        public string Feature { get; set; }
        public double ReadyAt { get; set; }
        public double Generation { get; set; }
    }
}
