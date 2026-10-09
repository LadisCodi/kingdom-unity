using System.Collections.Generic;
using System.Linq;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Research;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // The nav bar's doors: which tabs open and what each opens, and the bar stepping aside while a menu is
    // open, and the count of presses worth making behind a door. Persistent. Until the doors arrive, Build and
    // Research are the doors open.
    public class NavMenuPresenter : AbstractMenuPresenter<NavMenu>
    {
        private const string BUILD = "build";
        private const string RESEARCH = "research";

        private static readonly HashSet<string> OPEN = new() { BUILD, RESEARCH };

        private readonly UIManager _ui;
        private readonly Researching _research;
        private readonly ITreasury _treasury;

        public NavMenuPresenter(IMenuViewFactory views, UIManager ui, Researching research, ITreasury treasury) : base(views)
        {
            _ui = ui;
            _research = research;
            _treasury = treasury;
        }

        protected override void BindInternal(NavMenu view)
        {
            foreach (var tab in view.Tabs) tab.SetLocked(!OPEN.Contains(tab.Id));
            _ui.MenuWillShow += OnMenuWillShow;
            _ui.MenuHidden += OnMenuHidden;
            _research.Poured += OnPoured;
            _research.Researched += OnResearched;
            _treasury.Changed += OnTreasuryChanged;
            ShowBadges();
        }

        protected override void UnbindInternal(NavMenu view)
        {
            _ui.MenuWillShow -= OnMenuWillShow;
            _ui.MenuHidden -= OnMenuHidden;
            _research.Poured -= OnPoured;
            _research.Researched -= OnResearched;
            _treasury.Changed -= OnTreasuryChanged;
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
            else if (id == RESEARCH) _ = _ui.ShowMenu<ResearchMenu>();
        }

        private void OnPoured(string id, double amount) => ShowBadges();
        private void OnResearched(string id) => ShowBadges();
        private void OnTreasuryChanged(string currency, double amount) => ShowBadges();

        private void ShowBadges() => View.Tabs.FirstOrDefault(t => t.Id == RESEARCH)?.SetBadge(_research.ActionableCount());
    }
}
