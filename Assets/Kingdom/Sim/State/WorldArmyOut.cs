using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // An army out on the board.
    public sealed class WorldArmyOut
    {
        public string Id { get; set; }
        public List<string> Heroes { get; set; }
        public List<WorldTroops> Troops { get; set; }
        public double Target { get; set; }
        public string Purpose { get; set; }
    }
}
