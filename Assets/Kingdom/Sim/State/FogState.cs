using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // The fog: what is revealed, discovered and part-paid, and its treasures.
    public sealed class FogState
    {
        public Dictionary<string, bool> Revealed { get; set; }
        public Dictionary<string, bool> Discovered { get; set; }
        // Cell key → taps spent so far.
        public Dictionary<string, double> Progress { get; set; }
        public double PaidReveals { get; set; }
        public double TreasuresPlaced { get; set; }
        public Dictionary<string, Treasure> Treasures { get; set; }
    }
}
