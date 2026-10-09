using System.Collections.Generic;

namespace Codigames.Kingdom.Heroes.State
{
    // The heroes the kingdom owns and how far each has climbed; the fragments of every hero, owned or not; the wounds a
    // fight left; the hero slots bought.
    public class HeroesState
    {
        public List<string> Owned { get; set; } = new();
        public Dictionary<string, int> Levels { get; set; } = new();
        public Dictionary<string, int> Ascension { get; set; } = new();
        public Dictionary<string, int> Fragments { get; set; } = new();
        public Dictionary<string, int> SkillRanks { get; set; } = new();
        public Dictionary<string, HeroWound> Hurt { get; set; } = new();
        public int SlotsPurchased { get; set; }
    }

    // A share of the bar missing as of a moment, mending linearly from it; a hero a fight took to 0 is exhausted until
    // the bar is whole again.
    public class HeroWound
    {
        public double Missing { get; set; }
        public double At { get; set; }
        public bool Exhausted { get; set; }
    }
}
