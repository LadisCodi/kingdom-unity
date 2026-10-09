using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // A figure the level moves: its icon and name, its value now and, in green (clay when worse), what the level adds.
    public sealed class UpgradeRowData
    {
        public UpgradeRowData(Sprite icon, string label, string value, string delta, bool worse)
        {
            Icon = icon;
            Label = label;
            Value = value;
            Delta = delta;
            Worse = worse;
        }

        public Sprite Icon { get; }
        public string Label { get; }
        public string Value { get; }
        public string Delta { get; }
        public bool Worse { get; }
    }
}
