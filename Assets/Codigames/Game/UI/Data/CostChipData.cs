using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // One currency of a price: its icon, the amount, and whether the kingdom is short of it.
    public sealed class CostChipData
    {
        public CostChipData(Sprite icon, string amount, bool isShort)
        {
            Icon = icon;
            Amount = amount;
            IsShort = isShort;
        }

        public Sprite Icon { get; }
        public string Amount { get; }
        public bool IsShort { get; }
    }
}
