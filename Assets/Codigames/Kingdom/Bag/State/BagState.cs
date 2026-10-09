using System.Collections.Generic;

namespace Codigames.Kingdom.Bag.State
{
    // What the Bag holds, by item id; which are new since their tile was last tapped; and how many have come in since
    // the Bag was last opened (the nav's orb).
    public class BagState
    {
        public Dictionary<string, int> Held { get; set; } = new();
        public HashSet<string> Fresh { get; set; } = new();
        public int Badge { get; set; }

        // The boosts running.
        public List<RunningBoost> Boosts { get; set; } = new();
    }
}
