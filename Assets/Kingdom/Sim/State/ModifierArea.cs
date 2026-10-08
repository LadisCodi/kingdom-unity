using Kingdom.Sim.Core;

namespace Kingdom.Sim.State
{
    // Where a zone modifier applies: Chebyshev, inclusive of the centre.
    public sealed class ModifierArea
    {
        public Coord Centre { get; set; }
        public double Radius { get; set; }
        public Coord Size { get; set; }
        public string Relic { get; set; }
        public double Since { get; set; }
    }
}
