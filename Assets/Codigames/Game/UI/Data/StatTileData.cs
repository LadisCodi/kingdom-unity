using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // One figure on a building card's band: its icon, its short name, its value, and whether it is bad news.
    public sealed class StatTileData
    {
        public StatTileData(Sprite icon, string label, string value, bool bad = false)
        {
            Icon = icon;
            Label = label;
            Value = value;
            Bad = bad;
        }

        public Sprite Icon { get; }
        public string Label { get; }
        public string Value { get; }
        public bool Bad { get; }
    }
}
