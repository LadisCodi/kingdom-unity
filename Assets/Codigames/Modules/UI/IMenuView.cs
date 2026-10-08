using System.Threading.Tasks;

namespace Codigames.Modules.UI
{
    // What a menu's view does for its presenter: appear, disappear, and know when it is the top-most menu.
    // How it looks and animates is the view's business.
    public interface IMenuView
    {
        Task Show();

        Task Hide();

        void OnFocusGained();

        void OnFocusLost();
    }
}
