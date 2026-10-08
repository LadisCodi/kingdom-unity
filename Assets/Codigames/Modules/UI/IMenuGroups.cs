using System;

namespace Codigames.Modules.UI
{
    // Port: which menus form a group — opening one closes the other instead of covering it.
    public interface IMenuGroups
    {
        bool AreGrouped(Type a, Type b);
    }
}
