using System;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Store
{
    // What a store card shows: a name over its picture, the lines of what it gives, and its price in dollars.
    public sealed class StoreCardData
    {
        public string Name;
        public Sprite Art;
        public string Lines;
        public string Price;
    }

    // A product framed (the web's stx-card): a Gem pack or a bundle for the Bag. The whole card buys, as its button does.
    public class StoreCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text _name;
        [SerializeField] private Image _art;
        [SerializeField] private TMP_Text _lines;
        [SerializeField] private KitButton _buy;
        [SerializeField] private Button _card;

        public event Action Tapped;

        public KitButton Buy => _buy;

        private void Awake()
        {
            _buy.onClick.AddListener(() => Tapped?.Invoke());
            if (_card != null) _card.onClick.AddListener(() => Tapped?.Invoke());
        }

        public void Show(StoreCardData data)
        {
            _name.text = data.Name;
            _art.sprite = data.Art;
            _art.enabled = data.Art != null;
            _lines.gameObject.SetActive(!string.IsNullOrEmpty(data.Lines));
            _lines.text = data.Lines ?? string.Empty;
            _buy.Label = data.Price;
        }
    }
}
