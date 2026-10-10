using System;
using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // A confirmation asked over another screen: its title, picture, question, a note, and its two presses.
    public sealed class ConfirmData
    {
        public string Title;
        public Sprite Art;
        public string Text;
        public string Note;
        public string CancelLabel;
        public string OkLabel;
        public Action Ok;
    }
}
