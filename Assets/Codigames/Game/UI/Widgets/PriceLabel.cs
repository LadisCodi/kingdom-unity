using System.Collections.Generic;
using Codigames.Game.UI.Data;
using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Widgets
{
    // A price: one label (the Price role) with its icons inline — over a button, or spaced wide in a buy box (an
    // upgrade's, a technology's). Hidden when there is nothing to pay.
    [RequireComponent(typeof(TMP_Text))]
    public class PriceLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private bool _wide;

        public void Show(IReadOnlyList<PriceTerm> price)
        {
            gameObject.SetActive(price.Count > 0);
            _text.text = PriceLine.Of(price, _wide);
        }
    }
}
