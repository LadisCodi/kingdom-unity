using System;
using System.Collections.Generic;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Store
{
    // A wide row's words: its picture, name and line, and its Gem price — or, when there is no more to buy, why.
    public sealed class StoreWideData
    {
        public string Name;
        public Sprite Art;
        public string Line;
        public IReadOnlyList<PriceTerm> Price = Array.Empty<PriceTerm>();
        public string Label;
        public bool Enabled;
        // Shown instead of the button: everything bought.
        public string Owned;
        // Neither button nor words: a row only to read.
        public bool Bare;
    }

    // A row the width of the page (the web's stx-wide): the relic fragments, another builder, another hero slot.
    public class StoreWide : MonoBehaviour
    {
        [SerializeField] private Image _art;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _line;
        [SerializeField] private CostButton _buy;
        [SerializeField] private TMP_Text _owned;

        public event Action Tapped;

        public CostButton Buy => _buy;

        private void Awake() => _buy.Button.onClick.AddListener(() => Tapped?.Invoke());

        public void Show(StoreWideData data)
        {
            _art.sprite = data.Art;
            _art.enabled = data.Art != null;
            _name.text = data.Name;
            _line.text = data.Line;
            var owned = !string.IsNullOrEmpty(data.Owned);
            _owned.gameObject.SetActive(owned);
            _owned.text = owned ? "<sprite name=\"tick\"> " + data.Owned : string.Empty;
            _buy.gameObject.SetActive(!owned && !data.Bare);
            if (owned || data.Bare) return;
            _buy.Button.Label = data.Label;
            _buy.Show(data.Price, data.Enabled);
        }
    }
}
