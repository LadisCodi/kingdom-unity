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
        // A troop's rank, struck on a coin over the portrait from II up; 0 for none.
        public int Rank { get; set; }
        // A troop's four numbers (attack, damage, defence, health); empty for villagers.
        public IReadOnlyList<StatTileData> Stats { get; set; } = new List<StatTileData>();
        // A hall's ranks to choose from, when it has more than one open or to come; empty for none.
        public IReadOnlyList<RankRowData> Ranks { get; set; } = new List<RankRowData>();
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

    // One rank in a hall's menu: its portrait and name, why it is shut, and its numbers in a line.
    public sealed class RankRowData
    {
        public string Troop { get; set; }
        public Sprite Bust { get; set; }
        public Vector2 BustShift { get; set; }
        public float BustScale { get; set; } = 1;
        public int Rank { get; set; }
        public string Name { get; set; }
        public string Locked { get; set; }
        public string Numbers { get; set; }
        public bool Picked { get; set; }
    }

    // The Infirmary's ward: the beds taken, and a row a wounded troop with the price and time of mending it.
    public sealed class WardData
    {
        public string Beds { get; set; }
        public string Empty { get; set; }
        public IReadOnlyList<WardRowData> Rows { get; set; } = new List<WardRowData>();
    }

    public sealed class WardRowData
    {
        public string Troop { get; set; }
        public Sprite Bust { get; set; }
        public Vector2 BustShift { get; set; }
        public float BustScale { get; set; } = 1;
        public int Rank { get; set; }
        public string Name { get; set; }
        public string Line { get; set; }
        public IReadOnlyList<PriceTerm> Price { get; set; }
        public string Gate { get; set; }
        public bool CanHeal { get; set; }
        public string HealLabel { get; set; }
    }
}
