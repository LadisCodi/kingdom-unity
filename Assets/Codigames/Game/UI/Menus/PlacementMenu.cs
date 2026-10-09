using System;
using System.Collections.Generic;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // Placing a building: a small window across the bottom, because the map is the screen here. Its band
    // carries the building's name and the close, which is the cancel; its body the picture, what it does and how
    // long it takes, and the priced Build. View only: the PlacementMenuPresenter fills it.
    public class PlacementMenu : Menu
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _close;
        [SerializeField] private Image _art;
        [SerializeField] private AspectRatioFitter _artFit;
        [SerializeField] private TMP_Text _promise;
        [SerializeField] private TMP_Text _wait;
        [SerializeField] private TMP_Text _reason;
        [SerializeField] private RectTransform _price;
        [SerializeField] private CostChip _chipPrefab;
        [SerializeField] private Button _build;
        [SerializeField] private Color _ordinalColor = new Color32(0xf4, 0xe4, 0xc1, 0xcc);

        private readonly List<CostChip> _chips = new();

        public event Action CloseTapped;
        public event Action BuildTapped;

        // What the tutorial's lines call its controls.
        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            CoachTarget.Tag(_build, "place-confirm");
        }

        public void Show(PlacementPanelData panel)
        {
            _title.text = string.IsNullOrEmpty(panel.Ordinal)
                ? panel.Name
                : $"{panel.Name}<size=75%><color=#{ColorUtility.ToHtmlStringRGBA(_ordinalColor)}> {panel.Ordinal}</color></size>";
            _art.sprite = panel.Art;
            _art.enabled = panel.Art != null;
            if (panel.Art != null) _artFit.aspectRatio = panel.Art.rect.width / panel.Art.rect.height;
            _promise.text = panel.Promise;
            _wait.text = panel.Wait;
            _wait.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(panel.Wait));
            _build.GetComponentInChildren<TMP_Text>().text = panel.Verb;
            _reason.text = panel.Reason;
            _reason.gameObject.SetActive(!string.IsNullOrEmpty(panel.Reason));
            _build.interactable = panel.CanBuild;

            for (var i = 0; i < panel.Price.Count; i++)
            {
                if (i == _chips.Count) _chips.Add(Instantiate(_chipPrefab, _price));
                _chips[i].gameObject.SetActive(true);
                _chips[i].Show(panel.Price[i]);
            }

            for (var i = panel.Price.Count; i < _chips.Count; i++) _chips[i].gameObject.SetActive(false);
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _build.onClick.AddListener(OnBuild);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _build.onClick.RemoveListener(OnBuild);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnBuild() => BuildTapped?.Invoke();
    }
}
