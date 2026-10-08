using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // Ruins repaired.
    public sealed class AbandonedState
    {
        public Dictionary<string, bool> Repaired { get; set; }
    }
}
