using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // The upgrade sheet (the web's upgradeSheet): what the building becomes, what the level gains, what it asks —
    // every requirement ticked or crossed — and the price, the wait and the button.
    public sealed class UpgradeSheetData
    {
        public string Title { get; set; }
        public Sprite From { get; set; }
        public Sprite To { get; set; }
        public string FromLevel { get; set; }
        public string ToLevel { get; set; }
        public IReadOnlyList<UpgradeRowData> Gains { get; set; } = new List<UpgradeRowData>();
        public IReadOnlyList<GateRowData> Gates { get; set; } = new List<GateRowData>();
        public IReadOnlyList<PriceTerm> Price { get; set; } = new List<PriceTerm>();
        public string Time { get; set; }
        public bool Locked { get; set; }

        // Upgrade, with the padlock before it while a requirement is unmet.
        public string Button { get; set; }
        public bool CanUpgrade { get; set; }

        // Why the button is off, under it; empty when it is on.
        public string Note { get; set; }
    }
}
