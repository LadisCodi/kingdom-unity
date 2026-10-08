using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // The player's side of the shared world board.
    public sealed class WorldState
    {
        public WorldBoard Board { get; set; }
        public List<double> Revealed { get; set; }
        public List<ExplorerTrip> Explorers { get; set; }
        public double ExplorersBought { get; set; }
        public double TripsSent { get; set; }
        public List<WorldBuild> Builds { get; set; }
        public double Sanctuaries { get; set; }
        public List<string> Chapels { get; set; }
        public List<WorldArmyOut> Armies { get; set; }
        public double EffectSeq { get; set; }
        public double PortalAnnounced { get; set; }
        public List<PortalPrize> PortalPrizes { get; set; }
    }
}
