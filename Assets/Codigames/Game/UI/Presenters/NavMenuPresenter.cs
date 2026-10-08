using System.Collections.Generic;
using Codigames.Game.UI.Menus;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // The nav bar's doors: which tabs open and what each opens, and the bar stepping aside while a menu is
    // open. Persistent. Until the doors arrive, Build is the one door open.
    public class NavMenuPresenter : AbstractMenuPresenter<NavMenu>
    {
        private const string BUILD = "build";

        private static readonly HashSet<string> OPEN = new() { BUILD };

        private readonly UIManager _ui;

        public NavMenuPresenter(IMenuViewFactory views, UIManager ui) : base(views)
        {
            _ui = ui;
        }

        protected override void BindInternal(NavMenu view)
        {
            foreach (var tab in view.Tabs) tab.SetLocked(!OPEN.Contains(tab.Id));
            _ui.MenuWillShow += OnMenuWillShow;
            _ui.MenuHidden += OnMenuHidden;
        }

        protected override void UnbindInternal(NavMenu view)
        {
            _ui.MenuWillShow -= OnMenuWillShow;
            _ui.MenuHidden -= OnMenuHidden;
        }

        protected override void SubscribeToViewEventsInternal(NavMenu view) => view.TabTapped += OnTab;

        protected override void UnsubscribeFromViewEventsInternal(NavMenu view) => view.TabTapped -= OnTab;

        // It leaves as a menu starts to open, and comes back once the last one has closed.
        private void OnMenuWillShow(IMenuPresenter menu)
        {
            if (menu is IClosableMenuPresenter) View.SetTucked(true);
        }

        private void OnMenuHidden(IMenuPresenter _) => View.SetTucked(_ui.HasOverlayOpen);

        private void OnTab(string id)
        {
            if (id == BUILD) _ = _ui.ShowMenu<BuildMenu>();
        }
    }
}
