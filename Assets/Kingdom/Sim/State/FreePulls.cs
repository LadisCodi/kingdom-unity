namespace Kingdom.Sim.State
{
    // A banner's free pulls today.
    public sealed class FreePulls
    {
        public double Day { get; set; }
        public double Used { get; set; }
        public double ReadyAt { get; set; }
    }
}
