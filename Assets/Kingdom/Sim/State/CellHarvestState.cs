namespace Kingdom.Sim.State
{
    // A cell's depot: what is left, and when it recovers.
    public sealed class CellHarvestState
    {
        public double Units { get; set; }
        public double? ExhaustedUntil { get; set; }
        public double? RecoveryMs { get; set; }
        public bool? Growing { get; set; }
    }
}
