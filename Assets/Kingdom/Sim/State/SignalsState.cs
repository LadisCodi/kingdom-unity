using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // The playtest's signals.
    public sealed class SignalsState
    {
        public Dictionary<string, double> SightedAt { get; set; }
        public Dictionary<string, double> DiscoveredAt { get; set; }
        public double TreasureWaitMs { get; set; }
        public List<ReturnTap> ReturnTaps { get; set; }
        public double PlayMs { get; set; }
    }
}
