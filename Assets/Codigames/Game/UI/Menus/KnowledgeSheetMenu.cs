using System;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The Knowledge sheet: the bar, when the next point drips in, and three offers side by side — one point
    // for Gold, one for Gems, ten for Gems — each the amount over a button that is its price. View only: the
    // KnowledgeSheetMenuPresenter fills it.
    public class KnowledgeSheetMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private Button _scrim;
        [SerializeField] private TMP_Text _hint;
        [SerializeField] private ProgressBar _bar;
        [SerializeField] private TMP_Text _note;
        [SerializeField] private Offer[] _offers = new Offer[3];

        public event Action CloseTapped;

        // An offer's button: its index (0 one for Gold, 1 one for Gems, 2 ten for Gems).
        public event Action<int> OfferTapped;

        // What the tutorial's lines call its controls.
        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
        }

        public void Show(string hint, float fraction, string bar, string note)
        {
            _hint.text = hint;
            _bar.Set(fraction, bar);
            _note.text = note;
        }

        public void ShowOffer(int index, string count, string price, bool affordable)
        {
            _offers[index].Count.text = "<size=62%><sprite name=\"Knowledge\"></size>" + count;
            _offers[index].Price.text = "<sprite name=\"" + _offers[index].Currency + "\">" + price;
            _offers[index].Button.interactable = affordable;
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _scrim.onClick.AddListener(OnClose);
            for (var i = 0; i < _offers.Length; i++)
            {
                var index = i;
                _offers[i].Handler ??= () => OfferTapped?.Invoke(index);
                _offers[i].Button.onClick.AddListener(_offers[i].Handler);
            }
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _scrim.onClick.RemoveListener(OnClose);
            foreach (var offer in _offers) offer.Button.onClick.RemoveListener(offer.Handler);
        }

        private void OnClose() => CloseTapped?.Invoke();

        [Serializable]
        private class Offer
        {
            public TMP_Text Count;
            public TMP_Text Price;
            public Button Button;

            // The coin it is paid in: its icon goes in front of the price.
            public string Currency;

            [NonSerialized] public UnityEngine.Events.UnityAction Handler;
        }
    }
}
