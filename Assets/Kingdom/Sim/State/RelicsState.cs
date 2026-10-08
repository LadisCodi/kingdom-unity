using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // Relic fragments held, and the Shrines bought.
    public sealed class RelicsState
    {
        public Dictionary<string, RelicFragments> Held { get; set; }
        public double Chests { get; set; }
        public double PremiumShrines { get; set; }
    }
}
