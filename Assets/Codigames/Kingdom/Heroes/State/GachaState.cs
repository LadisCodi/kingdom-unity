using System.Collections.Generic;

namespace Codigames.Kingdom.Heroes.State
{
    // Per banner: the calls made (they key every roll), the calls since the last hero and since the last Legendary,
    // and the free calls an ad pays for.
    public class GachaState
    {
        public Dictionary<string, int> PullCounts { get; set; } = new();
        public Dictionary<string, int> PityCounters { get; set; } = new();
        public Dictionary<string, int> LegendaryPity { get; set; } = new();
        public Dictionary<string, FreePulls> FreePulls { get; set; } = new();
    }

    // Which UTC day the count belongs to, how many of that day's are gone, and when the next is offered.
    public class FreePulls
    {
        public long Day { get; set; }
        public int Used { get; set; }
        public double ReadyAt { get; set; }
    }
}
