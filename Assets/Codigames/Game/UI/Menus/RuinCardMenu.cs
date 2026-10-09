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
        [SerializeField] private RectTransform _price;
        [SerializeField] private CostChip _chipPrefab;
        [SerializeField] private Button _repair;

        private readonly List<CostChip> _chips = new();

        public event Action CloseTapped;
        public event Action RepairTapped;

        // What the tutorial's lines call its controls.
        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            CoachTarget.Tag(_repair, "repair");
        }

        public void Show(string title, Sprite art, string promise, string need, IReadOnlyList<CostChipData> price)
        {
            _title.text = title;
            _art.sprite = art;
            if (art != null) _artFit.aspectRatio = art.rect.width / art.rect.height;
            _promise.text = promise;
            _need.SetActive(!string.IsNullOrEmpty(need));
            _needText.text = need;

            for (var i = 0; i < price.Count; i++)
            {
                if (i == _chips.Count) _chips.Add(Instantiate(_chipPrefab, _price));
                _chips[i].gameObject.SetActive(true);
                _chips[i].Show(price[i]);
            }

            for (var i = price.Count; i < _chips.Count; i++) _chips[i].gameObject.SetActive(false);
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _scrim.onClick.AddListener(OnClose);
            _repair.onClick.AddListener(OnRepair);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _scrim.onClick.RemoveListener(OnClose);
            _repair.onClick.RemoveListener(OnRepair);
        }

        private void OnClose() => CloseTapped?.Invoke();

        private void OnRepair() => RepairTapped?.Invoke();
    }
}
