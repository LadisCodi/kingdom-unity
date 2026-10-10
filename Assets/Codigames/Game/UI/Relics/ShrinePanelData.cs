using System.Collections.Generic;
using Codigames.Game.UI.Data;
using UnityEngine;

namespace Codigames.Game.UI.Relics
{
    // A Shrine's section of its card (the web's dc-chapel): the chapel's painting, empty and calling with a + while it
    // waits (the badge when a relic in the Bag could go there), the relic set in it once placed with its name, level and
    // effect over the painting; asleep, Activate and, short of Mana, a flask at its foot; awake, the window running down.
    public sealed class ShrinePanelData
    {
        public Sprite Painting;
        public bool Empty;
        public bool Placeable;
        public Sprite Relic;
        public RelicStatus Status;
        public string Head;
        public string Effect;
        public float AwakeFraction;
        public string AwakeLeft;
        public string ActivateLabel;
        public IReadOnlyList<PriceTerm> ActivatePrice;
        public string FlaskLabel;
        public string FlaskNote;
    }
}
