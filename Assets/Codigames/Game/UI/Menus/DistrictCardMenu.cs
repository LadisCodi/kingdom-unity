using System;
using System.Collections.Generic;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // One building's card: a window across the bottom with its name and level on the band, its art and what it
    // does, and then either the work under way or the priced Upgrade. View only: the DistrictCardMenuPresenter
    // fills it.
    public class DistrictCardMenu : Menu
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _close;
        [SerializeField] private Image _art;
        [SerializeField] private AspectRatioFitter _artFit;
        [SerializeField] private TMP_Text _level;
        [SerializeField] private TMP_Text _promise;
        [SerializeField] private GameObject _workRow;
        [SerializeField] private TMP_Text _work;
        [SerializeField] private Image _workFill;
        [SerializeField] private GameObject _upgradeRow;
        [SerializeField] private GameObject _nextRow;
        [SerializeField] private TMP_Text _next;
        [SerializeField] private TMP_Text _reason;
        [SerializeField] private RectTransform _price;
        [SerializeField] private CostChip _chipPrefab;
        [SerializeField] private Button _upgrade;
        [SerializeField] private Color _ordinalColor = new Color32(0xf4, 0xe4, 0xc1, 0xcc);

        private readonly List<CostChip> _chips = new();

        public event Action CloseTapped;
        public event Action UpgradeTapped;

        public void Show(DistrictCardData card)
        {
            _title.text = string.IsNullOrEmpty(card.Ordinal)
                ? card.Name
                : $"{card.Name}<size=75%><color=#{ColorUtility.ToHtmlStringRGBA(_ordinalColor)}> {card.Ordinal}</color></size>";
            _art.sprite = card.Art;
            _art.enabled = card.Art != null;
            if (card.Art != null) _artFit.aspectRatio = card.Art.rect.width / card.Art.rect.height;
            _level.text = card.Level;
            _promise.text = card.Promise;

            _workRow.SetActive(card.Working);
            _work.text = card.Work;
            _workFill.fillAmount = card.Progress;

            _upgradeRow.SetActive(!card.Working);
            _nextRow.SetActive(!card.Working && !string.IsNullOrEmpty(card.Next));
            _next.text = card.Next;
            _reason.text = card.Reason;
            _reason.gameObject.SetActive(!string.IsNullOrEmpty(card.Reason));
            _upgrade.gameObject.SetActive(card.Price.Count > 0);
            _upgrade.interactable = card.CanUpgrade;

            for (var i = 0; i < card.Price.Count; i++)
            {
                if (i == _chips.Count) _chips.Add(Instantiate(_chipPrefab, _price));
                _chips[i].gameObject.SetActive(true);
                _chips[i].Show(card.Price[i]);
            }

            for (var i = card.Price.Count; i < _chips.Count; i++) _chips[i].gameObject.SetActive(false);
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _upgrade.onClick.AddListener(OnUpgrade);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _upgrade.onClick.RemoveListener(OnUpgrade);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnUpgrade() => UpgradeTapped?.Invoke();
    }
}
