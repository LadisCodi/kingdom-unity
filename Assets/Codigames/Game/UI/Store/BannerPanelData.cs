using System.Collections.Generic;
using Codigames.Game.UI.Data;
using UnityEngine;

namespace Codigames.Game.UI.Store
{
    // One call's button: what sits over it — a price, or a word such as Free — its label, and whether it presses.
    public sealed class CallButtonData
    {
        public IReadOnlyList<PriceTerm> Price { get; set; } = new List<PriceTerm>();
        public string Note { get; set; }
        public string Label { get; set; }
        public bool Enabled { get; set; } = true;
    }

    // A banner on the store's Heroes tab (the web's sth-banner): its name on its plank, gold or blue, the Legendary
    // standing on the golden call, the free calls line and its two calls.
    public sealed class BannerPanelData
    {
        public string Name { get; set; }
        public bool Golden { get; set; }
        public Sprite Hero { get; set; }
        public string Free { get; set; }
        public CallButtonData One { get; set; }
        public CallButtonData Ten { get; set; }
    }
}
