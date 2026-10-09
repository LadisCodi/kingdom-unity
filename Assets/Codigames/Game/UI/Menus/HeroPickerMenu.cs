using System;
using System.Collections.Generic;
using Codigames.Game.UI.Battles;
using Codigames.Game.UI.Heroes;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // THE HERO PICKER (the web's heroPicker): the filter bar and the sort, the heroes three to a row scrolling on their
    // own, the slots asked for in a green panel fixed under the list, and Select. View only: the HeroPickerMenuPresenter
    // fills it.
    public class HeroPickerMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private List<KitButton> _filters = new();
        [SerializeField] private KitButton _sort;
        [SerializeField] private TMP_Text _sortLabel;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _grid;
        [SerializeField] private HeroCardView _cardPrefab;
        [SerializeField] private TMP_Text _none;
        [SerializeField] private HeadPanel _party;
        [SerializeField] private RectTransform _slots;
        [SerializeField] private HeroSlotView _slotPrefab;
        [SerializeField] private KitButton _select;

        private readonly List<HeroCardView> _cards = new();
        private readonly List<HeroSlotView> _slotViews = new();

        public event Action CloseTapped;
        public event Action<int> FilterTapped;
        public event Action SortTapped;
        public event Action<string> CardTapped;
        public event Action<int> SlotTapped;
        public event Action SelectTapped;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            CoachTarget.Tag(_select, "picker-select");
            _close.onClick.AddListener(() => CloseTapped?.Invoke());
            for (var i = 0; i < _filters.Count; i++)
            {
                var index = i;
                _filters[i].onClick.AddListener(() => FilterTapped?.Invoke(index));
            }

            _sort.onClick.AddListener(() => SortTapped?.Invoke());
            _select.onClick.AddListener(() => SelectTapped?.Invoke());
        }

        public void Show(string title, int filter, string sort, IReadOnlyList<HeroCardData> heroes, string none, string partyHead, string partyCount,
            IReadOnlyList<HeroSlotData> slots, string select)
        {
            _title.text = title;
            for (var i = 0; i < _filters.Count; i++) _filters[i].Latched = i == filter;
            _sortLabel.text = sort;
            for (var i = 0; i < heroes.Count; i++)
            {
                if (i == _cards.Count)
                {
                    var card = Instantiate(_cardPrefab, _grid);
                    card.Tapped += id => CardTapped?.Invoke(id);
                    _cards.Add(card);
                }

                _cards[i].gameObject.SetActive(true);
                _cards[i].Show(heroes[i]);
            }

            for (var i = heroes.Count; i < _cards.Count; i++) _cards[i].gameObject.SetActive(false);
            _none.gameObject.SetActive(heroes.Count == 0);
            _none.text = none;

            _party.Title = partyHead;
            _party.Trail = partyCount;
            for (var i = 0; i < slots.Count; i++)
            {
                if (i == _slotViews.Count)
                {
                    var slot = Instantiate(_slotPrefab, _slots);
                    var index = i;
                    slot.Tapped += () => SlotTapped?.Invoke(index);
                    _slotViews.Add(slot);
                }

                _slotViews[i].gameObject.SetActive(true);
                _slotViews[i].Show(slots[i]);
            }

            for (var i = slots.Count; i < _slotViews.Count; i++) _slotViews[i].gameObject.SetActive(false);
            _select.Label = select;
        }

        public void ScrollToTop() => _scroll.verticalNormalizedPosition = 1;
    }
}
