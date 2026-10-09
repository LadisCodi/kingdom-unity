using System.Collections.Generic;

namespace Codigames.Game.UI
{
    // A menu that reads its own purse: while it is the top-most menu the header's left side shows these rows — a
    // currency or an item counted in the Bag — instead of the coins (the web's contextual purse: Hero XP and Stardust
    // over the heroes, the keys over the banner).
    public interface IPurseMenu
    {
        IReadOnlyList<string> Purse { get; }
    }
}
