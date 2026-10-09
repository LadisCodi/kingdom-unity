using System.Collections.Generic;

namespace Codigames.Kingdom.Relics.State
{
    // Every relic's level (0 = not restored), the fragments held of each, the city relics' open windows (when each
    // ends), which relic each Shrine hosts (by the Shrine's district id), the packs opened and the Shrines bought for
    // Gems.
    public class RelicsState
    {
        public Dictionary<string, int> Levels { get; set; } = new();
        public Dictionary<string, RelicFragments> Held { get; set; } = new();
        public Dictionary<string, double> Windows { get; set; } = new();
        public Dictionary<string, string> Hosts { get; set; } = new();
        public int Packs { get; set; }
        public int PremiumShrines { get; set; }
    }

    // Six slots — five pieces and the keystone — counted found and bound (bought) apart.
    public class RelicFragments
    {
        public int[] Found { get; set; } = new int[6];
        public int[] Bound { get; set; } = new int[6];
    }
}
