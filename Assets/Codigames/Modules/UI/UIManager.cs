using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Codigames.Modules.UI
{
    // Opens and closes menus. Closable menus form a stack in open order (top = last): the top-most has focus
    // and is what a close request closes. Opening a menu covers the top one, or replaces it when both belong
    // to one group; closing the top reveals what it covered. Persistent menus never enter the stack.
    public class UIManager
    {
        private readonly Func<IEnumerable<IMenuPresenter>> _presenterSource;
        private readonly IMenuGroups _groups;
        private readonly List<IClosableMenuPresenter> _stack = new();
        private Dictionary<Type, IMenuPresenter> _presenters;

        // The presenters arrive lazily: a presenter may itself depend on the UIManager.
        public UIManager(Func<IEnumerable<IMenuPresenter>> presenterSource, IMenuGroups groups)
        {
            _presenterSource = presenterSource;
            _groups = groups;
        }

        public event Action<IMenuPresenter> MenuWillShow;
        public event Action<IMenuPresenter> MenuShown;
        public event Action<IMenuPresenter> MenuWillHide;
        public event Action<IMenuPresenter> MenuHidden;

        // True while a closable menu is open over the persistent ones.
        public bool HasOverlayOpen => _stack.Count > 0;

        private Dictionary<Type, IMenuPresenter> Presenters => _presenters ??= _presenterSource().ToDictionary(p => p.MenuType);

        private IClosableMenuPresenter Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        public bool IsShown<TView>() where TView : IMenuView => IsShown(typeof(TView));

        public bool IsShown(Type menuType) => Presenters.TryGetValue(menuType, out var presenter) && presenter.IsShown;

        // Closes the top-most closable menu through its own close flow; nothing when only persistent menus are up.
        public void CloseTopMost() => Top?.RequestClose();

        public Task ShowMenu<TView>() where TView : IMenuView => ShowMenu(typeof(TView));

        public Task ShowMenu(Type menuType)
        {
            var presenter = PresenterOf(menuType);
            return ShowInternal(presenter, presenter.Show());
        }

        public Task ShowMenu<TView, TData>(TData data) where TView : IMenuView
        {
            var presenter = PresenterOf(typeof(TView));

            if (presenter is not IDataMenuPresenter<TData> dataPresenter)
                throw new InvalidOperationException($"The presenter of {typeof(TView).Name} takes no data of type {typeof(TData).Name}.");

            return ShowInternal(presenter, dataPresenter.Show(data));
        }

        public Task HideMenu<TView>() where TView : IMenuView => HideMenu(typeof(TView));

        public async Task HideMenu(Type menuType)
        {
            if (!Presenters.TryGetValue(menuType, out var presenter)) return;

            if (presenter is IClosableMenuPresenter closable && _stack.Contains(closable))
            {
                await ExitStack(closable);
                return;
            }

            if (presenter.IsShown) await Close(presenter);
        }

        // Closes every closable menu at once: the top through its own hide, the ones it covered (already hidden)
        // without being revealed. Persistent menus stay.
        public async Task CloseAll()
        {
            var top = Top;
            if (top == null) return;

            _stack.Clear();
            top.OnFocusLost();
            await Close(top);
        }

        public async Task HideAllMenus()
        {
            _stack.Clear();

            foreach (var presenter in Presenters.Values.Where(p => p.IsShown).ToArray())
            {
                await Close(presenter);
            }
        }

        private async Task ShowInternal(IMenuPresenter presenter, Task showing)
        {
            MenuWillShow?.Invoke(presenter);

            if (presenter is IClosableMenuPresenter closable) await Task.WhenAll(showing, EnterStack(closable));
            else await showing;

            MenuShown?.Invoke(presenter);
        }

        // Makes `entering` the top: a top of the same group is replaced, any other is covered (hidden but kept,
        // to be revealed when `entering` closes). Returns the covered or replaced menu's hide, which plays
        // alongside `entering`'s show.
        private Task EnterStack(IClosableMenuPresenter entering)
        {
            var previous = Top;
            var covered = Task.CompletedTask;

            if (previous != null && previous != entering)
            {
                previous.OnFocusLost();

                if (_groups.AreGrouped(previous.MenuType, entering.MenuType))
                {
                    _stack.Remove(previous);
                    covered = Close(previous);
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

        // Removes `leaving` and closes it; when it was the top, the menu it covered becomes the top and is shown.
        private async Task ExitStack(IClosableMenuPresenter leaving)
        {
            var wasTop = Top == leaving;
            _stack.Remove(leaving);

            if (wasTop) leaving.OnFocusLost();

            var closing = Close(leaving);
            var revealed = wasTop ? Top : null;

            if (revealed != null)
            {
                revealed.OnFocusGained();
                await Task.WhenAll(closing, revealed.Show());
            }
            else
            {
                await closing;
            }
        }

        private async Task Close(IMenuPresenter presenter)
        {
            MenuWillHide?.Invoke(presenter);
            await presenter.Hide();
            MenuHidden?.Invoke(presenter);
        }

        private IMenuPresenter PresenterOf(Type menuType)
            => Presenters.TryGetValue(menuType, out var presenter)
                ? presenter
                : throw new InvalidOperationException($"No presenter registered for the menu {menuType.Name}.");
    }
}
