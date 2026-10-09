using System.Collections.Generic;
using Codigames.Game.UI.Data;
using Codigames.Kingdom.Heroes;
using UnityEngine;

namespace Codigames.Game.UI.Heroes
{
    // One hero's card window (the web's heroesSheet detail): the header's ribbon, name and type; the stage with the
    // stars, the stats and the ascension; the skill; the boon; and the level — or, not found yet, the fragments.
    public sealed class HeroSheetData
    {
        public string Name { get; set; }
        public HeroRarity Rarity { get; set; }
        public string RarityLabel { get; set; }
        public string UnitType { get; set; }
        public string TypeLabel { get; set; }
        public Sprite Art { get; set; }
        public bool Missing { get; set; }
        public int Ascension { get; set; }
        public int StepsPerStar { get; set; }
        public IReadOnlyList<HeroStat> Stats { get; set; }

        // The ascension over the stage's foot; null when fully ascended or not found.
        public HeroBuy Ascend { get; set; }

        public string SkillName { get; set; }
        public int SkillRank { get; set; }
        public int SkillTop { get; set; }
        public string SkillSays { get; set; }
        // What the next rank waits on (a padlock line), or its price and Upgrade; neither at the top rank.
        public string SkillNote { get; set; }
        public HeroBuy SkillBuy { get; set; }

        public string Boon { get; set; }

        // "Level 8 of 10" or "Fragments 12 of 25": the label, the number in bold, the bar.
        public string Read { get; set; }
        public float ReadShare { get; set; }
        // Beside the reading: a note, the stars to ascend to, or a price and its button.
        public string ReadNote { get; set; }
        public int? CapStars { get; set; }
        public HeroBuy ReadBuy { get; set; }
    }

    public sealed class HeroStat
    {
        public HeroStat(string icon, string label, string value)
        {
            Icon = icon;
            Label = label;
            Value = value;
        }

        public string Icon { get; }
        public string Label { get; }
        public string Value { get; }
    }

    // A price over its button: the button's words and material, and whether it can be pressed.
    public sealed class HeroBuy
    {
        public IReadOnlyList<PriceTerm> Price { get; set; }
        public string Label { get; set; }
        public Kit.ButtonMaterial Material { get; set; } = Kit.ButtonMaterial.Green;
        public bool Enabled { get; set; } = true;
    }
}
