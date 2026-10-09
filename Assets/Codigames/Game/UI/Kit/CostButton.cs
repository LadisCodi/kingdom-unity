using System.Collections.Generic;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Kit
{
    // A priced button (the web's k-btn.has-cost): the price above the slab, the two grouped on a small section so
    // they read as one thing. A gate keeps the button and turns it off: where the price would be, the padlock and
    // a few words say why. Being short of coin is not a gate — the price stays, its short figure in clay.
    public class CostButton : MonoBehaviour
    {
        [SerializeField] private KitButton _button;
        [SerializeField] private PriceLabel _price;
        [SerializeField] private TMP_Text _gate;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _rim;

        public KitButton Button => _button;

        // With nothing to pay (a move) the button stands bare, without its section.
        public void Show(IReadOnlyList<PriceTerm> price, bool interactable)
        {
            _gate.gameObject.SetActive(false);
            _price.Show(price);
            Frame(price.Count > 0);
            _button.interactable = interactable;
        }

        public void ShowGate(string reason)
        {
            _price.gameObject.SetActive(false);
            _gate.gameObject.SetActive(true);
            _gate.text = "<sprite name=\"padlock\">" + reason;
            Frame(true);
            _button.interactable = false;
        }

        private void Frame(bool shown)
        {
            if (_fill != null) _fill.enabled = shown;
            if (_rim != null) _rim.enabled = shown;
        }
    }
}
