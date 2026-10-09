using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // The placement panel, ready to show: the building, its wait on the ghost's plot, its price, and why the
    // Build button is off when it is.
    public sealed class PlacementPanelData
    {
        public PlacementPanelData(string name, string ordinal, Sprite art, string promise, string wait,
            IReadOnlyList<CostChipData> price, bool canBuild, string reason)
        {
            Name = name;
            Ordinal = ordinal;
            Art = art;
            Promise = promise;
            Wait = wait;
            Price = price;
            CanBuild = canBuild;
            Reason = reason;
        }

        public string Name { get; }
        public string Ordinal { get; }
        public Sprite Art { get; }
        public string Promise { get; }
        public string Wait { get; }
        public IReadOnlyList<CostChipData> Price { get; }
        public bool CanBuild { get; }

        // Empty when nothing stands in the way, or when the price says it already.
        public string Reason { get; }
    }
}
