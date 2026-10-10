using System;
using System.Collections.Generic;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Store;
using Codigames.Game.UI.Widgets;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The store (the web's storeSheet, mockups m95–m98): a screen of its own over a magic merchant's shop, soft and
    // dim, with a strip of wooden tabs and the close along the top over the open tab's page. The Heroes tab (the web's
    // storeHeroes): a carousel of the heroes drifting and fading one into the next, "Call for aid", the odds on a tap,
    // and the two calls. View only: the StoreMenuPresenter fills it.
    public class StoreMenu : Menu
    {
        private const float CAROUSEL_S = 4.2f;
        private const float DRIFT = 14 * 2.8f;

        [SerializeField] private Button _close;
        [SerializeField] private List<TabButton> _tabs = new();
        [SerializeField] private GameObject _heroesPage;
        [SerializeField] private GameObject _locked;
        [SerializeField] private TMP_Text _lockedText;
        [SerializeField] private Image _carousel;
        [SerializeField] private CanvasGroup _carouselGroup;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _odds;
        [SerializeField] private TMP_Text _oddsLabel;
        [SerializeField] private GameObject _oddsTip;
        [SerializeField] private TMP_Text _oddsText;
        [SerializeField] private List<BannerPanel> _banners = new();

        [Header("Supplies and Gems")]
        [SerializeField, Tooltip("A scrolling page each, filled from code.")] private ScrollRect _suppliesPage;
        [SerializeField] private ScrollRect _gemsPage;
        [SerializeField] private ScrollRect _offersPage;
        [SerializeField] private StoreOffer _offerPrefab;
        [SerializeField] private StoreDaily _dailyPrefab;
        [SerializeField] private StoreRibbon _ribbonPrefab;
        [SerializeField] private StoreCard _cardPrefab;
        [SerializeField] private StoreWide _widePrefab;
        [SerializeField, Tooltip("A row of cards: two across, or three for the Gem packs.")] private GridLayoutGroup _gridPrefab;

        private readonly List<Sprite> _bag = new();
        private IReadOnlyList<Sprite> _heroes = Array.Empty<Sprite>();
        private Sprite _showing;
        private Sequence _drift;
        private Vector2 _carouselRest;

        public event Action CloseTapped;
        public event Action<int> TabTapped;
        public event Action<int> OneTapped;
        public event Action<int> TenTapped;
        // A product's card pressed: its id.
        public event Action<string> ProductTapped;
        // A wide row pressed: its id (fragments, builder, heroSlot).
        public event Action<string> RowTapped;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            _close.onClick.AddListener(() => CloseTapped?.Invoke());
            _odds.onClick.AddListener(() => _oddsTip.SetActive(!_oddsTip.activeSelf));
            for (var i = 0; i < _tabs.Count; i++)
            {
                var index = i;
                _tabs[i].Tapped += () => TabTapped?.Invoke(index);
            }

            for (var i = 0; i < _banners.Count; i++)
            {
                var index = i;
                _banners[i].OneTapped += () => OneTapped?.Invoke(index);
                _banners[i].TenTapped += () => TenTapped?.Invoke(index);
            }

            if (_banners.Count > 0) CoachTarget.Tag(_banners[0].One.Button, "banner-call");
            _carouselRest = _carousel.rectTransform.anchoredPosition;
        }

        public void ShowTabs(IReadOnlyList<(string Label, bool Open, bool News)> tabs)
        {
            for (var i = 0; i < _tabs.Count; i++)
            {
                var shown = i < tabs.Count;
                _tabs[i].gameObject.SetActive(shown);
                if (!shown) continue;
                _tabs[i].SetLabel(tabs[i].Label);
                _tabs[i].SetOpen(tabs[i].Open);
                _tabs[i].SetBadge(tabs[i].News && !tabs[i].Open ? 1 : 0);
            }
        }

        // The Heroes tab, or what opens it.
        public void ShowLocked(string reason)
        {
            ShowPage(null);
            _locked.SetActive(true);
            _lockedText.text = "<sprite name=\"padlock\"> " + reason;
        }

        public void ShowHeroes(string title, string odds, string oddsText, IReadOnlyList<BannerPanelData> banners)
        {
            ShowPage(_heroesPage);
            _title.text = title;
            _oddsLabel.text = odds;
            _oddsText.text = oddsText;
            for (var i = 0; i < _banners.Count; i++)
            {
                _banners[i].gameObject.SetActive(i < banners.Count);
                if (i < banners.Count) _banners[i].Show(banners[i]);
            }
        }

        // Every hero once, in a shuffled order, before any comes round again; a new bag never opens on the hero the
        // last one closed on. A picture, so Unity's randomness is fine.
        public void SetCarousel(IReadOnlyList<Sprite> heroes)
        {
            _heroes = heroes;
            if (_drift == null || !_drift.IsActive()) Next();
        }

        public void HideOdds() => _oddsTip.SetActive(false);

        // ---- Supplies and Gems: shelves of ribbons, card grids and wide rows, rebuilt only when what they show moves

        private string _shown;

        private readonly List<(TMP_Text Label, double Until)> _countdowns = new();

        // `page`: "supplies", "gems" or "offers".
        public void ShowShelves(string page, IReadOnlyList<StoreShelf> shelves, string signature)
        {
            var scroll = page == "gems" ? _gemsPage : page == "offers" ? _offersPage : _suppliesPage;
            ShowPage(scroll.gameObject);
            var key = page + ":" + signature;
            if (_shown == key) return;
            var fresh = _shown == null || !_shown.StartsWith(page + ":");
            _shown = key;
            _countdowns.Clear();
            var content = scroll.content;
            for (var i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            foreach (var shelf in shelves)
            {
                if (!string.IsNullOrEmpty(shelf.Ribbon))
                {
                    var ribbon = Instantiate(_ribbonPrefab, content);
                    ribbon.Show(shelf.Ribbon, shelf.RibbonUntil != null);
                    if (shelf.RibbonUntil != null && ribbon.Countdown != null) _countdowns.Add((ribbon.Countdown, shelf.RibbonUntil.Value));
                }

                foreach (var offer in shelf.Offers)
                {
                    var view = Instantiate(_offerPrefab, content);
                    view.Show(offer.Data);
                    var id = offer.Id;
                    view.Tapped += () => ProductTapped?.Invoke(id);
                    if (offer.Data.ClosesAt != null && view.Countdown != null) _countdowns.Add((view.Countdown, offer.Data.ClosesAt.Value));
                }

                if (shelf.Dailies.Count > 0)
                {
                    var grid = Instantiate(_gridPrefab, content);
                    grid.constraintCount = 3;
                    var width = ((RectTransform)content).rect.width - content.GetComponent<VerticalLayoutGroup>().padding.horizontal;
                    if (width <= 0) width = ((RectTransform)scroll.transform).rect.width;
                    grid.cellSize = new Vector2((width - grid.padding.horizontal - grid.spacing.x * 2) / 3, shelf.CardHeight);
                    foreach (var daily in shelf.Dailies)
                    {
                        var view = Instantiate(_dailyPrefab, grid.transform);
                        view.Show(daily.Data);
                        var id = daily.Id;
                        view.Tapped += () => ProductTapped?.Invoke(id);
                    }
                }

                if (shelf.Cards.Count > 0)
                {
                    var grid = Instantiate(_gridPrefab, content);
                    grid.constraintCount = shelf.Columns;
                    var width = ((RectTransform)content).rect.width - content.GetComponent<VerticalLayoutGroup>().padding.horizontal;
                    if (width <= 0) width = ((RectTransform)scroll.transform).rect.width;
                    var cell = (width - grid.padding.horizontal - grid.spacing.x * (shelf.Columns - 1)) / shelf.Columns;
                    grid.cellSize = new Vector2(cell, shelf.CardHeight);
                    foreach (var card in shelf.Cards)
                    {
                        var view = Instantiate(_cardPrefab, grid.transform);
                        view.Show(card.Data);
                        var id = card.Id;
                        view.Tapped += () => ProductTapped?.Invoke(id);
                    }
                }

                foreach (var row in shelf.Rows)
                {
                    var view = Instantiate(_widePrefab, content);
                    view.Show(row.Data);
                    var id = row.Id;
                    view.Tapped += () => RowTapped?.Invoke(id);
                }
            }

            WireClicks(content.gameObject);
            if (fresh) scroll.verticalNormalizedPosition = 1f;
        }

        // Every countdown on the page, written in place: the page is not rebuilt for the clock.
        public void TickCountdowns(double now, Func<double, string> format)
        {
            foreach (var (label, until) in _countdowns)
                if (label != null) label.text = format(Math.Max(0, Math.Ceiling((until - now) / 1000)));
        }

        private void ShowPage(GameObject page)
        {
            _locked.SetActive(false);
            _heroesPage.SetActive(page == _heroesPage);
            _suppliesPage.gameObject.SetActive(page == _suppliesPage.gameObject);
            _gemsPage.gameObject.SetActive(page == _gemsPage.gameObject);
            _offersPage.gameObject.SetActive(page == _offersPage.gameObject);
            if (page == _heroesPage) _shown = null;
        }

        protected override void PostShowInternal()
        {
            if (_heroes.Count > 0 && (_drift == null || !_drift.IsActive())) Next();
        }

        private void OnDisable()
        {
            _drift?.Kill();
            _drift = null;
        }

        private void Next()
        {
            if (_heroes.Count == 0 || !isActiveAndEnabled) return;
            if (_bag.Count == 0)
            {
                _bag.AddRange(_heroes);
                for (var i = _bag.Count - 1; i > 0; i--)
                {
                    var j = UnityEngine.Random.Range(0, i + 1);
                    (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
                }

                if (_bag.Count > 1 && _bag[0] == _showing) (_bag[0], _bag[1]) = (_bag[1], _bag[0]);
            }

            _showing = _bag[0];
            _bag.RemoveAt(0);
            _carousel.sprite = _showing;
            var rect = _carousel.rectTransform;
            rect.anchoredPosition = _carouselRest + new Vector2(DRIFT, 0);
            _carouselGroup.alpha = 0;
            _drift = DOTween.Sequence()
                .Append(rect.DOAnchorPos(_carouselRest - new Vector2(DRIFT, 0), CAROUSEL_S).SetEase(Ease.Linear))
                .Insert(0, _carouselGroup.DOFade(1, CAROUSEL_S * 0.16f))
                .Insert(CAROUSEL_S * 0.84f, _carouselGroup.DOFade(0, CAROUSEL_S * 0.16f))
                .SetUpdate(true)
                .OnComplete(Next);
        }
    }
}
