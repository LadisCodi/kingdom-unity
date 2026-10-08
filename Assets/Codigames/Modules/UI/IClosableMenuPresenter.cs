namespace Codigames.Modules.UI
{
    // A menu the player can close: it goes on the UIManager's stack. Persistent menus (a header) never do.
    public interface IClosableMenuPresenter : IMenuPresenter
    {
        void RequestClose();
    }
}
