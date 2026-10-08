using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // The Bag.
    public sealed class BagState
    {
        public Dictionary<string, double> Held { get; set; }
        public Dictionary<string, bool> Fresh { get; set; }
        public double Badge { get; set; }
    }
}
