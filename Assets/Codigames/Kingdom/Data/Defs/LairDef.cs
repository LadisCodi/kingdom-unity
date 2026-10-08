using Codigames.Kingdom.Core;

namespace Codigames.Kingdom.Data
{
    // A lair: one garrison, a path of fights, cleared once.
    public sealed class LairDef
    {
        public string Id { get; set; }
        public Coord Location { get; set; }
        public int Size { get; set; }
        public double Tier { get; set; }
        // Its zone past the footprint, in rings.
        public double Radius { get; set; }
        public double Sight { get; set; }
        public string Flavour { get; set; }
        public GuardDef Guard { get; set; }
    }
}
