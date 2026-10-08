using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // The roster.
    public sealed class HeroesState
    {
        public List<string> Owned { get; set; }
        public Dictionary<string, double> Levels { get; set; }
        public Dictionary<string, double> Ascension { get; set; }
        public Dictionary<string, double> Fragments { get; set; }
        public Dictionary<string, double> SkillRanks { get; set; }
        public double HeroSlotsPurchased { get; set; }
        public Dictionary<string, HeroHurt> Hurt { get; set; }
    }
}
