using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // A building's card, ready to show (the web's districtCard): its name and level on the band, its art and what it
    // is, the one thing bought for it — Upgrade, or the gem Finish while it is being built — the band of what it is
    // worth now, and the blocks of what it does: its training, its crew.
    public sealed class DistrictCardData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public Sprite Art { get; set; }
        public string What { get; set; }
        public bool Movable { get; set; }

        // Upgrade is shown below the top level; ready = every gate and the price met (its call-to-action badge).
        public bool Upgradable { get; set; }
        public bool UpgradeReady { get; set; }

        // The construction under way, in Upgrade's place; null otherwise.
        public WorkData Work { get; set; }

        public IReadOnlyList<StatTileData> Stats { get; set; } = new List<StatTileData>();

        public string TrainingHead { get; set; }
        public TrainingPanelData Training { get; set; }

        public string CrewHead { get; set; }
        public CrewPanelData Crew { get; set; }
        // Speed up's words on a timer's button, its hourglass inline.
        public string SpeedUp { get; set; }

        // Harmony: what a decoration supplies, or on the Townhall the city's supply and demand; null for none.
        public string HarmonyHead { get; set; }
        public string HarmonyLine { get; set; }
        public string HarmonyNote { get; set; }

        // What its neighbours do to it, as verdicts: a heading (none for a house's rent) and a badge each.
        public string NeighboursHead { get; set; }
        public IReadOnlyList<(string Text, bool Good)> Neighbours { get; set; } = new List<(string, bool)>();
    }
}
