namespace Kingdom.Sim.State
{
    // A relic's window and cooldown.
    public sealed class ArtifactCast
    {
        public double EndsAt { get; set; }
        public double ReadyAt { get; set; }
    }
}
