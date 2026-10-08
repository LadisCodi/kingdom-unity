using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // A relic's fragments: found and bound slots.
    public sealed class RelicFragments
    {
        public List<double> Found { get; set; }
        public List<double> Bound { get; set; }
    }
}
