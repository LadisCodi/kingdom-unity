using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Doors;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Quests;
using Codigames.Kingdom.Research;
using Codigames.Modules.Audio;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // The nav bar: each tab locked behind its door until the kingdom grows into it (a tap on a shut one says what
    // opens it), what each open tab opens, the bar stepping aside while a menu is open, and the count of presses
    // worth making behind a door. Persistent.
    public class NavMenuPresenter : AbstractMenuPresenter<NavMenu>
    {
        private const string BUILD = "build";
        private const string RESEARCH = "research";
        private const string BAG = "bag";
        private const string HEROES = "heroes";

        // Each tab's door; a tab with none here stays shut (its system is not in the game yet).
        private static readonly Dictionary<string, DoorId> DOORS = new()
        {
            [BUILD] = DoorId.Build, [RESEARCH] = DoorId.Research, ["heroes"] = DoorId.Heroes, ["bag"] = DoorId.Bag, ["store"] = DoorId.Store,
        };

        private readonly UIManager _ui;
        private readonly Researching _research;
        private readonly ITreasury _treasury;
        private readonly Doors _doors;
        private readonly Openings _openings;
        private readonly QuestChain _chain;
        private readonly Construction _construction;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;
        private readonly IQuickInfoMessageService _messages;
        private readonly Codigames.Kingdom.Bag.Bag _bag;

        public NavMenuPresenter(IMenuViewFactory views, UIManager ui, Researching research, ITreasury treasury, Doors doors, Openings openings,
            QuestChain chain, Construction construction, Localizer localizer, ISoundService sounds, IQuickInfoMessageService messages,
            Codigames.Kingdom.Bag.Bag bag) : base(views)
        {
            _bag = bag;
            _ui = ui;
            _research = research;
            _treasury = treasury;
            _doors = doors;
            _openings = openings;
            _chain = chain;
            _construction = construction;
            _localizer = localizer;
            _sounds = sounds;
            _messages = messages;
        }

        protected override void BindInternal(NavMenu view)
        {
            ShowDoors();
            _ui.MenuWillShow += OnMenuWillShow;
            _ui.MenuHidden += OnMenuHidden;
            _research.Poured += OnPoured;
            _research.Researched += OnResearched;
            _treasury.Changed += OnTreasuryChanged;
            _chain.Claimed += OnClaimed;
            _construction.DistrictPlaced += OnPlaced;
            _construction.JobCompleted += OnJobCompleted;
            _openings.Opened += OnOpened;
            _bag.Granted += OnGranted;
            ShowBadges();
        }

        protected override void UnbindInternal(NavMenu view)
        {
            _ui.MenuWillShow -= OnMenuWillShow;
            _ui.MenuHidden -= OnMenuHidden;
            _research.Poured -= OnPoured;
            _research.Researched -= OnResearched;
            _treasury.Changed -= OnTreasuryChanged;
            _chain.Claimed -= OnClaimed;
            _construction.DistrictPlaced -= OnPlaced;
            _construction.JobCompleted -= OnJobCompleted;
            _openings.Opened -= OnOpened;
            _bag.Granted -= OnGranted;
        }

        private void OnGranted(string id, int count) => ShowBadges();

        protected override void SubscribeToViewEventsInternal(NavMenu view) => view.TabTapped += OnTab;

        protected override void UnsubscribeFromViewEventsInternal(NavMenu view) => view.TabTapped -= OnTab;

        // It leaves as a menu starts to open, and comes back once the last one has closed.
        private void OnMenuWillShow(IMenuPresenter menu)
        {
            if (menu is IClosableMenuPresenter) View.SetTucked(true);
        }

        private void OnMenuHidden(IMenuPresenter _)
        {
            View.SetTucked(_ui.HasOverlayOpen);
            ShowBadges();
        }

        private void OnTab(string id)
        {
            if (!IsOpen(id))
            {
                _sounds.Play(SoundIds.ERROR);
                _messages.Show(new QuickInfoMessageData(Hint(id)));
                return;
            }

            if (id == BUILD) _ = _ui.ShowMenu<BuildMenu>();
            else if (id == RESEARCH) _ = _ui.ShowMenu<ResearchMenu>();
            else if (id == BAG) _ = _ui.ShowMenu<BagMenu>();
            else if (id == HEROES) _ = _ui.ShowMenu<HeroesMenu>();
            else if (id == "store") _ = _ui.ShowMenu<StoreMenu, string>(StoreMenuPresenter.HEROES);
        }

        private void OnPoured(string id, double amount) => ShowBadges();
        private void OnResearched(string id) => ShowDoors();
        private void OnTreasuryChanged(string currency, double amount) => ShowBadges();
        private void OnClaimed(IQuestDefinition claimed, IQuestDefinition next) => ShowDoors();
        private void OnPlaced(DistrictState district) => ShowDoors();
        private void OnJobCompleted(ConstructionJob job, DistrictState district) => ShowDoors();
        private void OnOpened(IReadOnlyList<DoorId> doors, IReadOnlyList<string> books) => ShowDoors();

        // Every tab to its door. A door that has just opened is remembered open by Openings, which announces it.
        private void ShowDoors()
        {
            foreach (var tab in View.Tabs) tab.SetLocked(!IsOpen(tab.Id));
            ShowBadges();
        }

        private bool IsOpen(string tab) => DOORS.TryGetValue(tab, out var door) && _doors.IsOpen(door);

        private void ShowBadges()
        {
            View.Tabs.FirstOrDefault(t => t.Id == RESEARCH)?.SetBadge(IsOpen(RESEARCH) ? _research.ActionableCount() : 0);
            // What turned up since the Bag was last opened.
            View.Tabs.FirstOrDefault(t => t.Id == BAG)?.SetBadge(IsOpen(BAG) ? _bag.Badge : 0);
        }

        // What opens a shut tab, in the player's words.
        private string Hint(string tab) => tab switch
        {
            RESEARCH => _localizer.Tr("Finish your first task to open this."),
            BUILD => _localizer.Tr("Settle a second villager to open this."),
            "heroes" => _localizer.Tr("Build a Tavern to open this."),
            "store" => _localizer.Tr("Raise the Townhall to level 2 to open this."),
            "bag" => _localizer.Tr("Find a chest to open this."),
            _ => string.Empty,
        };
    }
}
