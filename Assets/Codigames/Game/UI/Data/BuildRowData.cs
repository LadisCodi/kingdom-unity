using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // One row of the build menu, ready to show.
    public sealed class BuildRowData
    {
        public BuildRowData(string id, string name, string ordinal, string promise, Sprite art,
            IReadOnlyList<CostChipData> price, string wait, string built, bool available)
        {
            Id = id;
            Name = name;
            Ordinal = ordinal;
            Promise = promise;
            Art = art;
            Price = price;
            Wait = wait;
            Built = built;
            Available = available;
        }

        public string Id { get; }
        public string Name { get; }

        // "#3", or empty where only one may ever stand.
        public string Ordinal { get; }

        public string Promise { get; }
        public Sprite Art { get; }
        public IReadOnlyList<CostChipData> Price { get; }
        public string Wait { get; }
        public string Built { get; }

        // False when it cannot be built now: the row is drawn as such and refuses a tap.
        public bool Available { get; }
    }
}
