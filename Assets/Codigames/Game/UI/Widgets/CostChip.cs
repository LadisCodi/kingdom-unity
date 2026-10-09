using Codigames.Game.UI.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Widgets
{
    // One currency of a price in a list row: a parchment pill holding the icon and figure (one label), its rim
    // in clay when short.
    public class CostChip : MonoBehaviour
    {
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private Image _rim;
        [SerializeField] private Color _rimColor = new Color32(92, 58, 30, 115);
        [SerializeField] private Color _shortRimColor = new Color32(0xd4, 0x55, 0x3e, 0xff);

        public void Show(PriceTerm term)
        {
            _amount.text = PriceLine.Of(term);
            _rim.color = term.IsShort ? _shortRimColor : _rimColor;
        }
    }
}
