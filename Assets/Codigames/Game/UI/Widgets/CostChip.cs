using Codigames.Game.UI.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Widgets
{
    // A price: the currency's icon and the amount, in clay when short. Two prefabs share it: the chip
    // (a parchment pill whose rim turns clay, in a list row) and the plain cost over a button, set at the
    // header coin's size.
    public class CostChip : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private Image _rim;
        [SerializeField] private Color _rimColor = new Color32(92, 58, 30, 115);
        [SerializeField] private Color _shortRimColor = new Color32(0xd4, 0x55, 0x3e, 0xff);
        [SerializeField] private Color _amountColor = new Color32(0x3b, 0x24, 0x12, 0xff);
        [SerializeField] private Color _shortAmountColor = new Color32(0xd4, 0x55, 0x3e, 0xff);

        public void Show(CostChipData chip)
        {
            _icon.sprite = chip.Icon;
            _amount.text = chip.Amount;
            _amount.color = chip.IsShort ? _shortAmountColor : _amountColor;
            if (_rim != null) _rim.color = chip.IsShort ? _shortRimColor : _rimColor;
        }
    }
}
