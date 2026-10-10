using System;
using System.Collections.Generic;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Relics;
using Codigames.Game.UI.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The relic picker (the web's renderRelicPicker): the hero picker's frame with one slot — the restored city relics
    // as cards, three a row, scrolling on their own, each with what it does, the Shrine mark on those already in one and
    // the check on the chosen; the Shrine's slot in a green head panel fixed under the list; Select. View only: the
    // RelicPickerMenuPresenter fills it.
    public class RelicPickerMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private SectionHead _listHead;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _grid;
        [SerializeField] private RelicCardView _cardPrefab;
        [SerializeField] private TMP_Text _none;
        [SerializeField] private HeadPanel _slotPanel;
        [SerializeField] private GameObject _emptySlot;
        [SerializeField] private RelicCardView _slotCard;
        [SerializeField] private KitButton _select;

        private readonly List<RelicCardView> _cards = new();

        public event Action CloseTapped;
        public event Action<string> CardTapped;
        public event Action SlotTapped;
        public event Action SelectTapped;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            CoachTarget.Tag(_select, "relic-pick-select");
            _slotCard.Tapped += _ => SlotTapped?.Invoke();
        }

        public void ScrollToTop() => _scroll.verticalNormalizedPosition = 1;

        public void Show(string title, string listHead, IReadOnlyList<RelicCardData> cards, string none, string slotTitle, string slotCount,
            RelicCardData chosen, string select)
        {
            _title.text = title;
            _listHead.Title = listHead;
            _none.gameObject.SetActive(cards.Count == 0);
            _none.text = none;
            for (var i = 0; i < cards.Count; i++)
            {
                if (i == _cards.Count)
                {
                    var view = Instantiate(_cardPrefab, _grid);
                    view.Tapped += id => CardTapped?.Invoke(id);
                    _cards.Add(view);
                }

                _cards[i].gameObject.SetActive(true);
                _cards[i].Show(cards[i]);
                CoachTarget.Tag(_cards[i], "relic-pick:" + cards[i].Id);
            }

            for (var i = cards.Count; i < _cards.Count; i++) _cards[i].gameObject.SetActive(false);
            _slotPanel.Title = slotTitle;
            _slotPanel.Trail = slotCount;
            _emptySlot.SetActive(chosen == null);
            _slotCard.gameObject.SetActive(chosen != null);
            if (chosen != null) _slotCard.Show(chosen);
            _select.Label = select;
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _select.onClick.AddListener(OnSelect);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _select.onClick.RemoveListener(OnSelect);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnSelect() => SelectTapped?.Invoke();
    }
}
