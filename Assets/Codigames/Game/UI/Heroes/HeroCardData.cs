using Codigames.Kingdom.Heroes;
using UnityEngine;

namespace Codigames.Game.UI.Heroes
{
    // A hero as a card (the web's heroCard): the art on its rarity's face, what it fights as, its stars and the foot's
    // pill — its level, its power in a picker, its fragments when not found yet, its rest when exhausted — its HP when
    // hurt, its skill's rank past the first, the check when seated and the orb when something can be done with it.
    public sealed class HeroCardData
    {
        public string Id { get; set; }
        public Sprite Art { get; set; }
        public HeroRarity Rarity { get; set; }
        public bool Missing { get; set; }
        public string UnitType { get; set; }
        public int Ascension { get; set; }
        public int StepsPerStar { get; set; }
        public string Pill { get; set; }
        public Sprite PillIcon { get; set; }
        public bool PillReady { get; set; }
        // A share of the bar, or null when unhurt.
        public float? Hp { get; set; }
        public int Rank { get; set; }
        public bool Picked { get; set; }
        public bool Cta { get; set; }
        public bool Resting { get; set; }
    }
}
