namespace Kingdom.Sim.State
{
    // Rewarded ads.
    public sealed class AdsState
    {
        public double ReadyAt { get; set; }
        public double Claims { get; set; }
        public bool Pending { get; set; }
        public AdRefills Refills { get; set; }
    }
}
