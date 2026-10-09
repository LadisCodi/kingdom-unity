using System.Collections.Generic;
using Codigames.Game.UI.Data;
using UnityEngine;

namespace Codigames.Game.UI.Relics
{
    // What the level section's press is: nothing, Restore, or Level up.
    public enum RelicPress
    {
        None,
        Restore,
        LevelUp,
    }

    // A Shrine a relic could go to: which, and what its button's note says.
    public sealed class RelicHostChoice
    {
        public string ShrineId;
        public string Note;
    }

    // A relic's sheet (the web's renderRelicSheet), everything already in the player's words.
    public sealed class RelicSheetData
    {
        public string Title;
        public Sprite Art;
        public RelicStatus Status;
        public string AwakeRibbon;
        public string Story;
        public IReadOnlyList<StatTileData> Stats;
        public string Pending;

        // The level section: its head, the six slots, the note over the press, the press, where fragments come from.
        public string LevelHead;
        public IReadOnlyList<RelicSlotData> Slots;
        public string SetNote;
        public RelicPress Press;
        public string PressLabel;
        public IReadOnlyList<PriceTerm> PressPrice;
        public bool PressGreen;
        public bool PressBlocked;
        public string Sources;

        // The Shrine section, for a restored city relic: where it is hosted (muted when nowhere), the Shrines it could
        // go to, and whether it can be taken out. Null head: no section.
        public string ShrineHead;
        public string HostLine;
        public bool HostMuted;
        public string HostLabel;
        public IReadOnlyList<RelicHostChoice> HostChoices;
        public string RemoveLabel;
        public bool CanRemove;

        // The activation, once hosted: asleep, the wax, Activate and what a window buys (and, short of Mana, how soon
        // and a flask); awake, the window running down.
        public bool Activation;
        public bool Awake;
        public float AwakeFraction;
        public string AwakeLeft;
        public string ActivationNote;
        public string AsleepWax;
        public string ActivateLabel;
        public IReadOnlyList<PriceTerm> ActivatePrice;
        public string ManaNote;
        public string FlaskNote;
        public string FlaskLabel;

        public string StoreLabel;
    }
}
