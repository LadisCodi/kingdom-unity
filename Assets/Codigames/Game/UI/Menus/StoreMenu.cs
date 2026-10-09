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

        private readonly List<Sprite> _bag = new();
        private IReadOnlyList<Sprite> _heroes = Array.Empty<Sprite>();
        private Sprite _showing;
        private Sequence _drift;
        private Vector2 _carouselRest;

        public event Action CloseTapped;
        public event Action<int> TabTapped;
        public event Action<int> OneTapped;
        public event Action<int> TenTapped;

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
            _heroesPage.SetActive(false);
            _locked.SetActive(true);
            _lockedText.text = "<sprite name=\"padlock\"> " + reason;
        }

        public void ShowHeroes(string title, string odds, string oddsText, IReadOnlyList<BannerPanelData> banners)
        {
            _locked.SetActive(false);
            _heroesPage.SetActive(true);
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
