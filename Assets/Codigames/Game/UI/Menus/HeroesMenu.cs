using System;
using System.Collections.Generic;
using Codigames.Game.UI.Heroes;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The Heroes screen's roster (the web's heroesSheet grid): the filter bar — All, a tab per unit type, the sort — how
    // many are found, the cards three to a row scrolling on their own, and under them the way to the banner. View only:
    // the HeroesMenuPresenter fills it.
    public class HeroesMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private List<KitButton> _filters = new();
        [SerializeField] private KitButton _sort;
        [SerializeField] private TMP_Text _sortLabel;
        [SerializeField] private TMP_Text _found;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _grid;
        [SerializeField] private HeroCardView _cardPrefab;
        [SerializeField] private TMP_Text _none;
        [SerializeField] private KitButton _call;

        private readonly List<HeroCardView> _cards = new();

        public event Action CloseTapped;
        public event Action<int> FilterTapped;
        public event Action SortTapped;
        public event Action<string> CardTapped;
        public event Action CallTapped;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            _close.onClick.AddListener(() => CloseTapped?.Invoke());
            for (var i = 0; i < _filters.Count; i++)
            {
                var index = i;
                _filters[i].onClick.AddListener(() => FilterTapped?.Invoke(index));
            }

            _sort.onClick.AddListener(() => SortTapped?.Invoke());
            _call.onClick.AddListener(() => CallTapped?.Invoke());
            CoachTarget.Tag(_call, "heroes-call");
        }

        public void ShowBar(int filter, string sort)
        {
            for (var i = 0; i < _filters.Count; i++) _filters[i].Latched = i == filter;
            _sortLabel.text = sort;
        }

        public void ShowFound(string found) => _found.text = found;

        public void ShowCall(string label) => _call.Label = label;

        public void ShowCards(IReadOnlyList<HeroCardData> heroes, string none)
        {
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
        }

        public void ScrollToTop() => _scroll.verticalNormalizedPosition = 1;
    }
}
