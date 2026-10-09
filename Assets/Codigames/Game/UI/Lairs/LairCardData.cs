using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Lairs
{
    // What a lair's card says, in the player's language.
    public sealed class LairCardData
    {
        public string Title;
        public Sprite Painting;
        public string Flavour;
        public bool Beaten;
        public string ClockLabel;
        public string ClockValue;
        public string ProgressHead;
        public int Fights;
        public int Won;
        public string PathLabel;
        public string RewardHead;
        public IReadOnlyList<(Sprite Icon, string Value, string Tag)> Chips;
        public string Action;
    }
}
