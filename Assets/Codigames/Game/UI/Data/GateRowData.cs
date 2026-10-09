using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // A requirement of the next level: its icon, what it asks, and whether it is met.
    public sealed class GateRowData
    {
        public GateRowData(Sprite icon, string label, bool met)
        {
            Icon = icon;
            Label = label;
            Met = met;
        }

        public Sprite Icon { get; }
        public string Label { get; }
        public bool Met { get; }
    }
}
