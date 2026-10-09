using UnityEngine;

namespace Codigames.Game.UI.Data.Research
{
    // A book's ribbon hanging under the page.
    public readonly struct BookmarkData
    {
        public BookmarkData(string tome, Sprite emblem, Color tint, bool open)
        {
            Tome = tome;
            Emblem = emblem;
            Tint = tint;
            Open = open;
        }

        public string Tome { get; }
        public Sprite Emblem { get; }
        public Color Tint { get; }

        // The book on the page: its ribbon hangs longer.
        public bool Open { get; }
    }
}
