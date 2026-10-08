using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // An explorer out on the board.
    public sealed class ExplorerTrip
    {
        public string Id { get; set; }
        public double Target { get; set; }
        public List<double> Path { get; set; }
        public double DepartedAt { get; set; }
        public List<double> StepMs { get; set; }
        public double WorkMs { get; set; }
        public double Radius { get; set; }
        public double? RevealedAt { get; set; }
    }
}
