using System;
using System.Collections.Generic;

namespace Codigames.Game.UI.Heroes
{
    // What the hero picker is asked for (the web's openHeroPicker): how many slots, who already holds them, its title,
    // whether it chooses for a fight (each hero's power in its pill), and what to do with the answer.
    public sealed class HeroPick
    {
        public int Slots { get; set; }
        public IReadOnlyList<string> Selected { get; set; } = Array.Empty<string>();
        public string Title { get; set; }
        public bool Fight { get; set; }
        public Action<IReadOnlyList<string>> Select { get; set; }
    }
}
