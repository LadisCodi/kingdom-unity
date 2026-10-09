using System.Collections.Generic;
using Codigames.Game.UI.Data;
using UnityEngine;

namespace Codigames.Game.UI.Relics
{
    // Where a relic stands: in fragments, awake over its aura, asleep in its Shrine, or held and acting nowhere.
    public enum RelicStatus
    {
        Broken,
        Awake,
        Asleep,
        Bag,
    }

    // One of a relic's six slots: its fragment's own art, how many are held (0: missing), and the keystone's bigger rim.
    public sealed class RelicSlotData
    {
        public Sprite Art;
        public int Count;
        public string CountText;
        public bool Keystone;
    }

    // One relic's card in the Bag (the web's relicCardTile), everything already in the player's words.
    public sealed class RelicCardData
    {
        public string Id;
        public string Name;
        public Sprite Art;
        public bool World;
        public RelicStatus Status;
        // "Lv 3", or null before it is restored.
        public string Level;
        public IReadOnlyList<RelicSlotData> Slots;
        // In fragments: "Ready to restore" when six distinct are held, else how many of the six.
        public string Ready;
        public string Held;
        // Awake: "Awake · 1h". Held and acting nowhere: "In the Bag".
        public string Awake;
        public string Where;
        // Asleep: the Mana waking it costs.
        public IReadOnlyList<PriceTerm> Activate;
        public string ActivateLabel;
    }

    // The Bag's Relics tab: the Shrines standing, then the city relics and the world relics under their heads.
    public sealed class RelicTabData
    {
        public string Shrines;
        public string ShrinesNote;
        public string CityHead;
        public string WorldHead;
        public IReadOnlyList<RelicCardData> City;
        public IReadOnlyList<RelicCardData> World;
    }
}
