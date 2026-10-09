using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Bag
{
    // What the picked item's popover says and offers (the web's bag-pop): its name and line, a second line when there
    // is one, a choice chest's four coins, the quantity when more than one is held, and its one action.
    public sealed class BagPopoverData
    {
        public string Name { get; set; }
        public string Line { get; set; }
        public string Note { get; set; }
        public IReadOnlyList<ChoicePlateData> Choice { get; set; }
        public int Max { get; set; }
        public int Quantity { get; set; } = 1;
        public string QuantityText { get; set; }
        public string Total { get; set; }
        public BagAction Action { get; set; }
        public string ActionLabel { get; set; }

        // Where the notch points: the picked tile's column, 0 to 3.
        public int Column { get; set; }
    }

    // What the popover's button does.
    public enum BagAction
    {
        None,
        Use,
        SpeedUp,
        Store,
    }

    // One coin of a choice chest: its icon, what it would give now, and whether it is the one picked.
    public sealed class ChoicePlateData
    {
        public string Coin { get; set; }
        public Sprite Icon { get; set; }
        public string Amount { get; set; }
        public bool Picked { get; set; }
    }
}
