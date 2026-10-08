using Kingdom.Sim.Core;

namespace Kingdom.Sim.Data
{
    // A landmark placed on the map.
    public sealed class LandmarkDef
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public Coord Location { get; set; }
        public double ClaimCost { get; set; }
        public int Size { get; set; }
    }
}
