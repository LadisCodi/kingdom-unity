using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // The Survey's claimed levels and whether its paid track is owned.
    public sealed class SurveyState
    {
        public List<double> ClaimedFree { get; set; }
        public List<double> ClaimedPaid { get; set; }
        public bool Owned { get; set; }
    }
}
