using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // Technologies done, Knowledge poured, band rewards claimed.
    public sealed class ResearchState
    {
        public List<string> Completed { get; set; }
        public Dictionary<string, double> Poured { get; set; }
        public List<string> Rewarded { get; set; }
    }
}
