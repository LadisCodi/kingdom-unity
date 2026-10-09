using System.Collections.Generic;
using Codigames.Game.UI.Data;
using UnityEngine;

namespace Codigames.Game.UI.Bag
{
    // The speed-up picker, ready to show (the web's speedupSheet, mockup M71): the timer — its icon, what it is, its
    // bar and the time left — Auto with what it would spend, one row per speed-up that fits, and Finish's Gems last.
    public sealed class SpeedupScreenData
    {
        public Sprite Icon { get; set; }
        public string Title { get; set; }
        public float Progress { get; set; }
        public string Left { get; set; }
        public string Auto { get; set; }
        public IReadOnlyList<SpeedupRowData> Rows { get; set; }
        public string None { get; set; }
        // A row's button, in the player's language.
        public string Use { get; set; }
        public IReadOnlyList<PriceTerm> Finish { get; set; }
        public bool CanFinish { get; set; }
    }

    public sealed class SpeedupRowData
    {
        public BagTileData Tile { get; set; }
        public string Name { get; set; }
    }
}
