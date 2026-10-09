using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // One row of the build menu, ready to show (the web's bld-card).
    public sealed class BuildRowData
    {
        public BuildRowData(string id, string name, string ordinal, string promise, Sprite art,
            IReadOnlyList<PriceTerm> price, string wait, string built, bool available, string why = null, bool known = true)
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
            Why = why;
            Known = known;
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

        // False when it cannot be built now: a tap shakes it.
        public bool Available { get; }

        // What keeps it shut — a technology, or the cap — in place of its promise and price; null when it is open.
        // A shut row wears the locked paper, its art drained and padlocked.
        public string Why { get; }

        // False while a technology has still to open it: no wait and no count yet.
        public bool Known { get; }

        public bool Blocked => !string.IsNullOrEmpty(Why);
    }
}
