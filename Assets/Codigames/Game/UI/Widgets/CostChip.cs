using Codigames.Game.UI.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Widgets
{
    // A price's chip: a parchment pill with the currency's icon and the amount, its rim in clay when short.
    public class CostChip : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private Image _rim;
        [SerializeField] private Color _rimColor = new Color32(92, 58, 30, 115);
        [SerializeField] private Color _shortRimColor = new Color32(0xd4, 0x55, 0x3e, 0xff);

        public void Show(CostChipData chip)
        {
            _icon.sprite = chip.Icon;
            _amount.text = chip.Amount;
            _rim.color = chip.IsShort ? _shortRimColor : _rimColor;
        }
    }
}
