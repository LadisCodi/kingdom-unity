using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace Codigames.Modules.UI
{
public class UIManager
{
    private readonly IObjectResolver _resolver;
    private readonly MenuCatalog _catalog;

    private Dictionary<Type, IMenuPresenter> _presenters;
    private Dictionary<Type, IMenuPresenter> Presenters =>
        _presenters ??= _resolver.Resolve<IEnumerable<IMenuPresenter>>().ToDictionary(p => p.MenuType);

    // Closeable menus in open order (top = last). Persistent menus (header, main, nav bar) aren't closeable
    // and never enter the stack. The top-most one has focus and is the target of a close request.
    private readonly List<IClosableMenuPresenter> _stack = new();

    public event Action<IMenuPresenter> MenuWillShow;
    public event Action<IMenuPresenter> MenuShown;
    public event Action<IMenuPresenter> MenuWillHide;
    public event Action<IMenuPresenter> MenuHidden;

    public UIManager(IObjectResolver resolver, MenuCatalog catalog)
    {
        _resolver = resolver;
        _catalog = catalog;
    }

    public bool IsShown<TMenu>() where TMenu : Menu => IsShown(typeof(TMenu));

    public bool IsShown(Type menuType) => Presenters.TryGetValue(menuType, out var presenter) && presenter.IsShown;

    // True while at least one closeable (overlay) menu is open on top of the persistent menus. Persistent
    // menus (header, main, nav bar) never enter the stack, so this is false on the plain main screen.
    public bool HasOverlayOpen => _stack.Count > 0;

    // Closes the top-most closeable menu through its own close flow (no-op if only persistent menus are up).
    // Invoked by the nav bar close button and by Escape / the Android back button.
    public void CloseTopMost()
    {
        if (_stack.Count > 0) _stack[_stack.Count - 1].RequestClose();
    }

    public UniTask ShowMenu<TMenu>() where TMenu : Menu => ShowMenu(typeof(TMenu));

    public async UniTask ShowMenu(Type menuType)
    {
        if (!TryGetPresenter(menuType, out var presenter)) return;

        await ShowInternal(presenter, presenter.Show());
    }

    public async UniTask ShowMenu<TMenu, TData>(TData data) where TMenu : Menu
    {
        if (!TryGetPresenter(typeof(TMenu), out var presenter)) return;

        if (presenter is not IMenuPresenter<TData> dataPresenter)
        {
            Debug.LogError($"Presenter for {typeof(TMenu).Name} does not accept data of type {typeof(TData).Name}.");
            return;
        }

        await ShowInternal(presenter, dataPresenter.Show(data));
    }

    private async UniTask ShowInternal(IMenuPresenter presenter, UniTask showing)
    {
        MenuWillShow?.Invoke(presenter);

        // Closeable menus go on the stack, covering or replacing whatever was on top; the cover/replace
        // animation plays alongside this menu's show.
        if (presenter is IClosableMenuPresenter closable)
            await UniTask.WhenAll(showing, EnterStack(closable));
        else
            await showing;

        MenuShown?.Invoke(presenter);
    }

    public UniTask HideMenu<TMenu>() where TMenu : Menu => HideMenu(typeof(TMenu));

    public async UniTask HideMenu(Type menuType)
    {
        if (!Presenters.TryGetValue(menuType, out var presenter)) return;

        if (presenter is IClosableMenuPresenter closable && _stack.Contains(closable))
        {
            await ExitStack(closable);
            return;
        }

        if (presenter.IsShown) await ClosePresenter(presenter);
    }

    public async UniTask HideAllMenus()
    {
        _stack.Clear();

        foreach (var presenter in Presenters.Values.Where(p => p.IsShown).ToArray())
        {
            await ClosePresenter(presenter);
        }
    }

    // Makes `entering` the top of the stack. A same-group top is replaced (closed and removed); a
    // different-group top is hidden but kept in the stack, to be revealed when `entering` is later closed.
    // Returns the covered/replaced menu's hide animation so it plays alongside `entering`'s show.
    private UniTask EnterStack(IClosableMenuPresenter entering)
    {
        var previous = Top;
        var covered = UniTask.CompletedTask;

        if (previous != null && previous != entering)
        {
            previous.OnFocusLost();

            if (AreGrouped(previous, entering))
            {
                _stack.Remove(previous);
                covered = ClosePresenter(previous);
            }
            else
            {
                covered = previous.Hide();
            }
        }

        if (!_stack.Contains(entering)) _stack.Add(entering);
        entering.OnFocusGained();

        return covered;
    }

    // Removes `leaving` from the stack and closes it. If it was the top, the menu it was covering becomes the
    // new top and is shown again.
    private async UniTask ExitStack(IClosableMenuPresenter leaving)
    {
        var wasTop = Top == leaving;
        _stack.Remove(leaving);

        if (wasTop) leaving.OnFocusLost();

        var closing = ClosePresenter(leaving);
        var revealed = wasTop ? Top : null;

        if (revealed != null)
        {
            revealed.OnFocusGained();
            await UniTask.WhenAll(closing, revealed.Show());
        }
        else
        {
            await closing;
        }
    }

    private async UniTask ClosePresenter(IMenuPresenter presenter)
    {
        MenuWillHide?.Invoke(presenter);
        await presenter.Hide();
        MenuHidden?.Invoke(presenter);
    }

    private IClosableMenuPresenter Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

    private bool AreGrouped(IMenuPresenter a, IMenuPresenter b)
        => _catalog.AreGrouped(a.MenuType, b.MenuType);

    private bool TryGetPresenter(Type menuType, out IMenuPresenter presenter)
    {
        if (Presenters.TryGetValue(menuType, out presenter)) return true;

        Debug.LogError($"No presenter registered for menu {menuType.Name}.");
        return false;
    }
}
}
