using System.Collections.Generic;
using System.Linq;
using Codigames.Game.UI.Heroes;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Heroes;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // The roster's logic (the web's heroesSheet grid and Game.heroesList): the owned heroes by level or rarity, then the
    // ones not found yet in roster order, under one type filter; the orb on each card worth opening; a tap opens the
    // hero's card in the roster's place.
    public class HeroesMenuPresenter : AbstractMenuPresenter<HeroesMenu>, IClosableMenuPresenter, IPurseMenu
    {
        private static readonly string[] PURSE = { Kingdom.Heroes.Heroes.HERO_XP, Kingdom.Heroes.Heroes.STARDUST };

        private readonly UIManager _ui;
        private readonly Kingdom.Heroes.Heroes _heroes;
        private readonly HeroCards _cards;
        private readonly ITreasury _treasury;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly List<string> _types;

        private int _filter;
        private bool _byRarity;

        public HeroesMenuPresenter(IMenuViewFactory views, UIManager ui, Kingdom.Heroes.Heroes heroes, HeroCards cards, ITreasury treasury,
            NumberFormat numbers, Localizer localizer, IClock clock) : base(views)
        {
            _ui = ui;
            _heroes = heroes;
            _cards = cards;
            _treasury = treasury;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            // The filter's tabs: the unit types heroes fight as, in roster order.
            _types = heroes.All.Select(h => h.UnitType).Distinct().ToList();
        }

        public IReadOnlyList<string> Purse => PURSE;

        public void RequestClose() => _ = _ui.HideMenu<HeroesMenu>();

        protected override void BindInternal(HeroesMenu view)
        {
            Refresh();
            _heroes.Changed += Refresh;
            _treasury.Changed += OnTreasury;
        }

        protected override void UnbindInternal(HeroesMenu view)
        {
            _heroes.Changed -= Refresh;
            _treasury.Changed -= OnTreasury;
        }

        protected override void SubscribeToViewEventsInternal(HeroesMenu view)
        {
            view.CloseTapped += RequestClose;
            view.FilterTapped += OnFilter;
            view.SortTapped += OnSort;
            view.CardTapped += OnCard;
        }

        protected override void UnsubscribeFromViewEventsInternal(HeroesMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.FilterTapped -= OnFilter;
            view.SortTapped -= OnSort;
            view.CardTapped -= OnCard;
        }

        private void OnTreasury(string currency, double amount)
        {
            if (currency is Kingdom.Heroes.Heroes.HERO_XP or Kingdom.Heroes.Heroes.STARDUST) Refresh();
        }

        private void OnFilter(int index)
        {
            _filter = index;
            Refresh();
            View.ScrollToTop();
        }

        private void OnSort()
        {
            _byRarity = !_byRarity;
            Refresh();
        }

        // The card opens in the roster's place; its close brings the roster back.
        private async void OnCard(string id)
        {
            await _ui.HideMenu<HeroesMenu>();
            _ = _ui.ShowMenu<HeroCardMenu, string>(id);
        }

        private void Refresh()
        {
            if (View == null) return;
            View.ShowBar(_filter, _byRarity ? _localizer.Tr("Rarity") : _localizer.Tr("Lv"));
            View.ShowFound(_localizer.Tr("{found} of {total} found", ("found", _numbers.Count(_heroes.Owned.Count)),
                ("total", _numbers.Count(_heroes.All.Count))));
            var now = _clock.NowMs;
            View.ShowCards(List().Select(id => _cards.Card(id, now, cta: _cards.Ready(id))).ToList(), _localizer.Tr("No heroes of that type"));
        }

        // The owned as the picker orders them, then the ones not found yet in roster order, under the type filter.
        private IEnumerable<string> List()
        {
            var type = _filter == 0 || _filter > _types.Count ? null : _types[_filter - 1];
            bool Shown(IHeroDefinition h) => type == null || h.UnitType == type;
            var owned = _heroes.Owned.Select(_heroes.Get).Where(Shown);
            owned = _byRarity
                ? owned.OrderByDescending(h => h.Rarity).ThenByDescending(h => _heroes.Level(h.Id))
                : owned.OrderByDescending(h => _heroes.Level(h.Id)).ThenByDescending(h => h.Rarity);
            return owned.Select(h => h.Id).Concat(_heroes.All.Where(h => Shown(h) && !_heroes.Owns(h.Id)).Select(h => h.Id));
        }
    }
}
