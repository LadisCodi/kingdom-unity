using System;
using System.Threading.Tasks;

namespace Codigames.Modules.UI
{
    // The logic behind one menu: what it shows and what it does. The menu's view type is its key.
    public interface IMenuPresenter
    {
        Type MenuType { get; }

        bool IsShown { get; }

        bool HasFocus { get; }

        Task Show();

        Task Hide();

        void OnFocusGained();

        void OnFocusLost();
    }
}
