using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // The placement panel, ready to show: the building, its wait on the ghost's plot, its price, and why the
    // Build button is off when it is.
    public sealed class PlacementPanelData
    {
        public PlacementPanelData(string name, string ordinal, Sprite art, string promise, string wait,
            IReadOnlyList<PriceTerm> price, bool canBuild, string reason, string verb)
        {
            Verb = verb;
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
        // Empty for a move of a building, which takes no time: the empty space is the message.
        public string Wait { get; }

        // The button's one verb: Build, or Move.
        public string Verb { get; }
        public IReadOnlyList<PriceTerm> Price { get; }
        public bool CanBuild { get; }

        // Empty when nothing stands in the way, or when the price says it already.
        public string Reason { get; }
    }
}
