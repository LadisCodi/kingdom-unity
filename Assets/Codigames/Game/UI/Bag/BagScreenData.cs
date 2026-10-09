using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Bag
{
    // The Bag, ready to show: its tabs, the open tab's items (or the line saying where they turn up), the running
    // boosts over the Boosts tab, and the picked item's popover.
    public sealed class BagScreenData
    {
        public IReadOnlyList<BagTabData> Tabs { get; set; }
        public IReadOnlyList<BagTileData> Items { get; set; }
        public string Empty { get; set; }
        public IReadOnlyList<BoostRibbonData> Ribbons { get; set; }
        public int Picked { get; set; } = -1;
        public BagPopoverData Popover { get; set; }
        // The Relics tab's cards, in place of items; null on every other tab.
        public Relics.RelicTabData Relics { get; set; }
    }

    public sealed class BagTabData
    {
        public string Label { get; set; }
        public bool Open { get; set; }
        public bool Empty { get; set; }
        public bool Fresh { get; set; }
    }

    // A boost running, as a ribbon: its icon, what it raises, and the time left.
    public sealed class BoostRibbonData
    {
        public Sprite Icon { get; set; }
        public string What { get; set; }
        public string Left { get; set; }
    }
}
