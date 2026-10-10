using System;
using System.Collections.Generic;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // An abandoned building's card: its name, its ruin on a tile, what it does once it stands again, what it is
    // missing, and Repair with its price. View only: the RuinCardMenuPresenter fills it.
    public class RuinCardMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private Button _scrim;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Image _art;
        [SerializeField] private AspectRatioFitter _artFit;
        [SerializeField] private TMP_Text _promise;
        [SerializeField] private GameObject _need;
        [SerializeField] private TMP_Text _needText;
        [SerializeField] private CostButton _repair;


        [SerializeField, Tooltip("Its window: where its top edge stands, for the camera.")] private MapCard _card;

        // The window's top edge, as a share of the screen's height from the bottom.
        public float CardTop() => _card.ViewportTop();

        public event Action CloseTapped;
        public event Action RepairTapped;

        // What the tutorial's lines call its controls.
        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            CoachTarget.Tag(_repair.Button, "repair");
        }

        // The building stands on its tile and rises out of it; a tall one (a tower) is held lower, so its top stays clear
        // of the title.
        private void FitArt(Sprite art)
        {
            if (_artHeight <= 0) _artHeight = ((RectTransform)_art.transform).sizeDelta.y;
            var aspect = art.rect.width / art.rect.height;
            var rect = (RectTransform)_art.transform;
            // Before its first layout the tile has no height yet: its preferred one stands in.
            var tile = ((RectTransform)rect.parent).rect.height;
            if (tile <= 0 && rect.parent.TryGetComponent<LayoutElement>(out var element)) tile = element.preferredHeight;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, aspect < 1 && tile > 0 ? Mathf.Min(_artHeight, tile * 1.25f) : _artHeight);
            _artFit.aspectRatio = aspect;
        }

        private float _artHeight;

        public void Show(string title, Sprite art, string promise, string need, IReadOnlyList<PriceTerm> price, bool affordable)
        {
            _title.text = title;
            _art.sprite = art;
            if (art != null) FitArt(art);
            _promise.text = promise;
            _need.SetActive(!string.IsNullOrEmpty(need));
            _needText.text = need;

            _repair.Show(price, affordable);
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _scrim.onClick.AddListener(OnClose);
            _repair.Button.onClick.AddListener(OnRepair);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _scrim.onClick.RemoveListener(OnClose);
            _repair.Button.onClick.RemoveListener(OnRepair);
        }

        private void OnClose() => CloseTapped?.Invoke();

        private void OnRepair() => RepairTapped?.Invoke();
    }
}
