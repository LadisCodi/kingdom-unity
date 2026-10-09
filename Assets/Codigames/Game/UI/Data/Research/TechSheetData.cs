using System.Collections.Generic;
using Codigames.Kingdom.Research;
using UnityEngine;

namespace Codigames.Game.UI.Data.Research
{
    // A technology, opened: what it is, and then what its state lets the player do — nothing once researched,
    // its requirements while locked, its Knowledge and its price while it can be worked on.
    public class TechSheetData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Sprite Icon { get; set; }
        public string Says { get; set; }
        public bool Planned { get; set; }
        public TechState State { get; set; }

        // Locked: one row per requirement, met or not.
        public IReadOnlyList<RequirementData> Requirements { get; set; } = new List<RequirementData>();

        // In progress: poured over needed, and the three pours (Gems for all that is missing, +1, +most).
        public float Fraction { get; set; }
        public string Bar { get; set; }
        public bool Filled { get; set; }
        public bool NeedsKnowledge { get; set; }
        public string GemsPrice { get; set; }
        public bool CanBuyWithGems { get; set; }
        public bool CanPour { get; set; }
        public string PourMost { get; set; }

        // The price paid when its Knowledge is in, and why the button is shut (empty when it is not).
        public IReadOnlyList<PriceTerm> Price { get; set; } = new List<PriceTerm>();
        public bool CanResearch { get; set; }

        // Research, as the button reads it.
        public string ResearchLabel { get; set; }
        public string Note { get; set; }
    }

    // What a requirement asks for: a technology researched, or cells revealed.
    public enum RequirementKind
    {
        Technology,
        Cells,
    }

    public readonly struct RequirementData
    {
        public RequirementData(RequirementKind kind, string label, bool met)
        {
            Kind = kind;
            Label = label;
            Met = met;
        }

        public RequirementKind Kind { get; }
        public string Label { get; }
        public bool Met { get; }
    }
}
