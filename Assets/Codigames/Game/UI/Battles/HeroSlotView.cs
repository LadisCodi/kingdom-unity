using System;
using Codigames.Game.UI.Heroes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Battles
{
    public enum HeroSlotKind
    {
        Hero,
        Empty,
        Locked,
    }

    // A hero slot on a board: a hero's card, an open slot to fill, or a slot to buy — only the next one carrying its
    // price, since the ladder climbs.
    public sealed class HeroSlotData
    {
        public HeroSlotKind Kind { get; set; }
        public HeroCardData Card { get; set; }
        public string Price { get; set; }
    }

    // One hero slot on the attack sheet (the web's bt-card): the hero's card at the board's size, the slot sunk into
    // the paper, or the padlock and the next slot's Gems.
    public class HeroSlotView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private HeroCardView _card;
        [SerializeField] private GameObject _empty;
        [SerializeField] private GameObject _locked;
        [SerializeField] private GameObject _price;
        [SerializeField] private TMP_Text _priceText;
        // The card is built at this width and scaled to the slot's.
        [SerializeField] private float _cardWidth = 302;

        public event Action Tapped;

        private void Awake() => _button.onClick.AddListener(() => Tapped?.Invoke());

        private void OnRectTransformDimensionsChange() => Fit();

        private void Fit()
        {
            var width = ((RectTransform)transform).rect.width;
            if (width > 0 && _card != null) _card.transform.localScale = Vector3.one * (width / _cardWidth);
        }

        public void Show(HeroSlotData slot)
        {
            _card.gameObject.SetActive(slot.Kind == HeroSlotKind.Hero);
            if (slot.Kind == HeroSlotKind.Hero) _card.Show(slot.Card);
            _empty.SetActive(slot.Kind == HeroSlotKind.Empty);
            _locked.SetActive(slot.Kind == HeroSlotKind.Locked);
            _price.SetActive(slot.Kind == HeroSlotKind.Locked && !string.IsNullOrEmpty(slot.Price));
            _priceText.text = slot.Price;
            Fit();
        }
    }
}
