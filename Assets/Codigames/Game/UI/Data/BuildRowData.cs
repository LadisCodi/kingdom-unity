using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // One row of the build menu, ready to show.
    public sealed class BuildRowData
    {
        public BuildRowData(string id, string name, string ordinal, string promise, Sprite art,
            IReadOnlyList<PriceTerm> price, string wait, string built, bool available, bool locked = false)
        {
            Locked = locked;
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
        public IReadOnlyList<PriceTerm> Price { get; }
        public string Wait { get; }
        public string Built { get; }

        // False when it cannot be built now: the row is drawn as such and refuses a tap.
        public bool Available { get; }

        // Behind a technology: a padlock on its art, and what opens it in place of its promise and its price.
        public bool Locked { get; }
    }
}
