using UnityEngine;

namespace Codigames.Game.UI.Bag
{
    // One item on the Bag's grid: its picture and rarity, the size printed over a borrowed picture, how many, a
    // speed-up's type badge, and whether it is new or picked.
    public sealed class BagTileData
    {
        public string Id { get; set; }
        public Sprite Icon { get; set; }
        public int Tier { get; set; }
        public string Size { get; set; }
        public string Count { get; set; }
        public Sprite Badge { get; set; }
        public bool Fresh { get; set; }
        public bool Picked { get; set; }
    }
}
