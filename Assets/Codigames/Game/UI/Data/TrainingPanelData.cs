using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // The training block (the web's trainingSection): who it trains — its bust and how many the city has, its tag and
    // its line — the amount one press orders, Train priced whole or gated with why, and the batch at its foot.
    public sealed class TrainingPanelData
    {
        public Sprite Bust { get; set; }
        public Vector2 BustShift { get; set; }
        public float BustScale { get; set; } = 1;
        public string Owned { get; set; }
        public string Tag { get; set; }
        public string Description { get; set; }
        public string Amount { get; set; }
        public IReadOnlyList<PriceTerm> Price { get; set; }
        public string Gate { get; set; }
        public bool CanTrain { get; set; }

        // Null when nothing is training.
        public WorkData Batch { get; set; }
        public string Empty { get; set; }
    }
}
