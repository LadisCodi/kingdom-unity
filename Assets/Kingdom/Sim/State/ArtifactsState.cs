using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // Relic levels, casts and charges.
    public sealed class ArtifactsState
    {
        public Dictionary<string, double> Levels { get; set; }
        public Dictionary<string, ArtifactCast> Casts { get; set; }
        public Dictionary<string, double> Charges { get; set; }
    }
}
