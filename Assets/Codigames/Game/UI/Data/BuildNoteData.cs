using UnityEngine;

namespace Codigames.Game.UI.Data
{
    public enum BuildNoteTone
    {
        Plain,
        // Harmony short of its demand: the line in clay.
        Short,
        // A surplus that pays: its note in leaf, strong.
        Paying,
    }

    // A line over a tab's rows (the web's bld-harmony and bld-tip): an icon, words, and a note at the far end.
    public sealed class BuildNoteData
    {
        public Sprite Icon { get; set; }
        public string Text { get; set; }
        public string Note { get; set; }
        public BuildNoteTone Tone { get; set; }
        public bool Strong { get; set; }
    }
}
