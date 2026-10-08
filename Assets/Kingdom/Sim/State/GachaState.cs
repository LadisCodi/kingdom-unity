using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // The banners' counters.
    public sealed class GachaState
    {
        public Dictionary<string, double> PullCounts { get; set; }
        public Dictionary<string, double> PityCounters { get; set; }
        public Dictionary<string, FreePulls> FreePulls { get; set; }
        public Dictionary<string, double> LegendaryPity { get; set; }
    }
}
