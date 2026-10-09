using System.Collections.Generic;
using Codigames.Kingdom.Heroes;
using UnityEngine;

namespace Codigames.Game.UI.Reveal
{
    // A fragments bar on a card or a bag row: where it stood, where the call left it, its readings, whether it filled
    // the recruiting price, and gold toward a recruit or blue toward a star.
    public sealed class RevealBar
    {
        public float From { get; set; }
        public float To { get; set; }
        public string FromText { get; set; }
        public string ToText { get; set; }
        public bool Recruited { get; set; }
        public bool Gold { get; set; }
    }

    public sealed class RevealRowData
    {
        public Sprite Art { get; set; }
        public bool Missing { get; set; }
        public Color Wash { get; set; }
        public string Name { get; set; }
        public string Count { get; set; }
        public Sprite CountIcon { get; set; }
        public RevealBar Bar { get; set; }
    }

    // One prize as a card (the web's prizeCard): its back — the ink as ornate as it is rare, glowing when a new hero
    // waits under it — and its face: a picture, a name and a count; a whole hero on its rarity's card; a family of
    // supplies; the bag's rows. Fragments carry their bar under the card.
    public sealed class RevealCardData
    {
        public PrizeKind Kind { get; set; }
        public int Ink { get; set; }
        public bool Charged { get; set; }
        public Color Glow { get; set; }
        public HeroRarity? Rarity { get; set; }
        public bool NewHero { get; set; }

        public Sprite Icon { get; set; }
        public Sprite Art { get; set; }
        public bool Missing { get; set; }
        public Color Wash { get; set; }
        public string Name { get; set; }
        public string Count { get; set; }
        public Sprite CountIcon { get; set; }

        public IReadOnlyList<(Sprite Icon, string Count)> Supplies { get; set; } = new List<(Sprite, string)>();
        public IReadOnlyList<RevealRowData> Rows { get; set; } = new List<RevealRowData>();
        public RevealBar Bar { get; set; }
    }

    // A whole reveal for the screen: the chest, the line under the plaque, the cards and what each new hero is called.
    public sealed class RevealScreenData
    {
        public RevealChest Chest { get; set; }
        public string Caption { get; set; }
        public IReadOnlyList<RevealCardData> Cards { get; set; } = new List<RevealCardData>();
        public IReadOnlyList<RevealHeroLine> Heroes { get; set; } = new List<RevealHeroLine>();
    }

    // What the room says for a new hero: the kicker over the card, and its name, title and rarity under it.
    public sealed class RevealHeroLine
    {
        public int Card { get; set; }
        public string Kicker { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Rarity { get; set; }
        public HeroRarity Tier { get; set; }
    }
}
