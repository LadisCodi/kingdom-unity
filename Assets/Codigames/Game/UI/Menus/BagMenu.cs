using System;
using System.Collections.Generic;
using Codigames.Game.UI.Bag;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Relics;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The Bag (the web's bagSheet, mockup M69): items held until they are used. Tabs over a grid of square tiles, four
    // a row; a tap on a tile opens its popover inside the grid, under that tile's row, pushing the rows below down, and
    // a second tap closes it. Over the Boosts tab, the boosts running. The Relics tab holds no items: the Shrines
    // standing, then a card for every relic met, two a row, city then world. View only: the BagMenuPresenter fills it.
    public class BagMenu : Menu
    {
        private const int COLUMNS = 4;

        [SerializeField] private Button _close;
        [SerializeField] private List<TabButton> _tabs = new();
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _ribbons;
        [SerializeField] private BoostRibbon _ribbonPrefab;
        [SerializeField] private RectTransform _grid;
        [SerializeField] private BagTile _tilePrefab;
        [SerializeField] private BagPopover _popover;
        [SerializeField] private TMP_Text _empty;
        [SerializeField] private float _gap = 22;

        [Header("Relics")]
        [SerializeField] private GameObject _relics;
        [SerializeField] private TMP_Text _shrines;
        [SerializeField] private TMP_Text _shrinesNote;
        [SerializeField] private SectionHead _cityHead;
        [SerializeField] private RectTransform _cityGrid;
        [SerializeField] private SectionHead _worldHead;
        [SerializeField] private RectTransform _worldGrid;
        [SerializeField] private RelicCardView _relicCardPrefab;
        [SerializeField] private float _relicGap = 28;

        private readonly List<RectTransform> _rows = new();
        private readonly List<BagTile> _tiles = new();
        private readonly List<BoostRibbon> _ribbonViews = new();
        private readonly List<RelicCardView> _cityCards = new();
        private readonly List<RelicCardView> _worldCards = new();
        private readonly List<RectTransform> _cityRows = new();
        private readonly List<RectTransform> _worldRows = new();

        public event Action CloseTapped;
        public event Action<int> TabTapped;
        public event Action<string> TileTapped;
        public event Action<string> RelicTapped;
        public event Action<string> RelicActivateTapped;

        public BagPopover Popover => _popover;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            for (var i = 0; i < _tabs.Count; i++)
            {
                var index = i;
                _tabs[i].Tapped += () => TabTapped?.Invoke(index);
            }
        }

        public void Show(BagScreenData bag)
        {
            for (var i = 0; i < _tabs.Count && i < bag.Tabs.Count; i++)
            {
                _tabs[i].SetLabel(bag.Tabs[i].Label);
                _tabs[i].SetOpen(bag.Tabs[i].Open);
                _tabs[i].SetDim(bag.Tabs[i].Empty && !bag.Tabs[i].Open);
                _tabs[i].SetBadge(bag.Tabs[i].Fresh ? 1 : 0);
            }

            ShowRibbons(bag.Ribbons);
            var relics = bag.Relics;
            var nothing = relics == null ? bag.Items.Count == 0 : relics.City.Count + relics.World.Count == 0;
            _empty.gameObject.SetActive(nothing);
            _empty.text = bag.Empty;
            _grid.gameObject.SetActive(relics == null);
            _relics.SetActive(relics != null && !nothing);
            if (relics == null) ShowGrid(bag);
            else if (!nothing) ShowRelics(relics);
        }

        private void ShowRelics(RelicTabData tab)
        {
            _shrines.text = "<sprite name=\"Shrine\">" + tab.Shrines;
            _shrinesNote.gameObject.SetActive(tab.ShrinesNote != null);
            _shrinesNote.text = tab.ShrinesNote ?? string.Empty;
            _cityHead.Title = tab.CityHead;
            _worldHead.Title = tab.WorldHead;
            Cards(_cityHead, _cityGrid, _cityRows, _cityCards, tab.City);
            Cards(_worldHead, _worldGrid, _worldRows, _worldCards, tab.World);
        }

        // Two a row, as wide as the list allows; a row as tall as its taller card.
        private void Cards(SectionHead head, RectTransform grid, List<RectTransform> rows, List<RelicCardView> views,
            IReadOnlyList<RelicCardData> cards)
        {
            head.gameObject.SetActive(cards.Count > 0);
            grid.gameObject.SetActive(cards.Count > 0);
            var width = (_scroll.viewport.rect.width - _relicGap) / 2;
            var count = (cards.Count + 1) / 2;
            while (rows.Count < count) rows.Add(NewRow(grid, _relicGap));
            for (var i = 0; i < cards.Count; i++)
            {
                if (i == views.Count)
                {
                    var view = Instantiate(_relicCardPrefab);
                    view.Tapped += id => RelicTapped?.Invoke(id);
                    view.ActivateTapped += id => RelicActivateTapped?.Invoke(id);
                    views.Add(view);
                }

                var card = views[i];
                card.transform.SetParent(rows[i / 2], false);
                card.transform.SetSiblingIndex(i % 2);
                var element = card.GetComponent<LayoutElement>();
                element.minWidth = element.preferredWidth = width;
                element.flexibleWidth = 0;
                card.gameObject.SetActive(true);
                card.Show(cards[i]);
            }

            for (var i = cards.Count; i < views.Count; i++) views[i].gameObject.SetActive(false);
            for (var i = 0; i < rows.Count; i++) rows[i].gameObject.SetActive(i < count);
        }

        public void ScrollToTop() => _scroll.verticalNormalizedPosition = 1;

        private void ShowRibbons(IReadOnlyList<BoostRibbonData> ribbons)
        {
            _ribbons.gameObject.SetActive(ribbons.Count > 0);
            for (var i = 0; i < ribbons.Count; i++)
            {
                if (i == _ribbonViews.Count) _ribbonViews.Add(Instantiate(_ribbonPrefab, _ribbons));
                _ribbonViews[i].gameObject.SetActive(true);
                _ribbonViews[i].Show(ribbons[i]);
            }

            for (var i = ribbons.Count; i < _ribbonViews.Count; i++) _ribbonViews[i].gameObject.SetActive(false);
        }

        // Rows of four, the popover after the picked tile's row.
        private void ShowGrid(BagScreenData bag)
        {
            // The viewport is anchored, so its width is right before the grid has been laid out once.
            var width = _scroll.viewport.rect.width;
            var side = (width - _gap * (COLUMNS - 1)) / COLUMNS;
            var rows = (bag.Items.Count + COLUMNS - 1) / COLUMNS;
            while (_rows.Count < rows) _rows.Add(NewRow(_grid, _gap));

            for (var i = 0; i < bag.Items.Count; i++)
            {
                if (i == _tiles.Count)
                {
                    var tile = Instantiate(_tilePrefab);
                    tile.Tapped += id => TileTapped?.Invoke(id);
                    _tiles.Add(tile);
                }

                var view = _tiles[i];
                view.transform.SetParent(_rows[i / COLUMNS], false);
                view.transform.SetSiblingIndex(i % COLUMNS);
                var element = view.GetComponent<LayoutElement>();
                element.minWidth = element.preferredWidth = element.minHeight = element.preferredHeight = side;
                view.gameObject.SetActive(true);
                view.Show(bag.Items[i]);
            }

            for (var i = bag.Items.Count; i < _tiles.Count; i++) _tiles[i].gameObject.SetActive(false);
            for (var i = 0; i < _rows.Count; i++)
            {
                _rows[i].gameObject.SetActive(i < rows);
                _rows[i].SetSiblingIndex(i);
            }

            var shown = bag.Popover != null && bag.Picked >= 0;
            _popover.gameObject.SetActive(shown);
            if (!shown) return;
            _popover.transform.SetSiblingIndex(bag.Picked / COLUMNS + 1);
            _popover.Show(bag.Popover);
        }

        private static RectTransform NewRow(RectTransform parent, float gap)
        {
            var row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = gap;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            // A relic row stretches both cards to the taller; a bag tile's row is fixed squares either way.
            layout.childForceExpandHeight = true;
            return row;
        }

        protected override void SubscribeToEventsInternal() => _close.onClick.AddListener(OnClose);
        protected override void UnsubscribeFromEventsInternal() => _close.onClick.RemoveListener(OnClose);

        private void OnClose() => CloseTapped?.Invoke();
    }
}
