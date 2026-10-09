using Codigames.Game.UI.Data.Research;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Research
{
    // One requirement, as the upgrade popup says it: its icon, what it asks, ticked once met — a pink row with
    // a cross until then. View only.
    public class RequirementRowView : MonoBehaviour
    {
        [SerializeField] private Image _tile;
        [SerializeField] private Image _rim;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _mark;
        [SerializeField] private Sprite _technology;
        [SerializeField] private Sprite _cells;
        [SerializeField] private Sprite _tick;
        [SerializeField] private Sprite _cross;
        [SerializeField] private Color _metFill = new Color32(0xf0, 0xd9, 0xae, 0xff);
        [SerializeField] private Color _metRim = new Color32(0xcf, 0xa8, 0x74, 0xff);
        [SerializeField] private Color _metInk = new Color32(0x3b, 0x24, 0x12, 0xff);
        [SerializeField] private Color _unmetFill = new Color32(0xf6, 0xdc, 0xd2, 0xff);
        [SerializeField] private Color _unmetRim = new Color32(0xe3, 0xb0, 0xa4, 0xff);
        [SerializeField] private Color _unmetInk = new Color32(0xd4, 0x55, 0x3e, 0xff);

        public void Show(RequirementData requirement)
        {
            _icon.sprite = requirement.Kind == RequirementKind.Technology ? _technology : _cells;
            _label.text = requirement.Label;
            _mark.sprite = requirement.Met ? _tick : _cross;
            _tile.color = requirement.Met ? _metFill : _unmetFill;
            _rim.color = requirement.Met ? _metRim : _unmetRim;
            _label.color = requirement.Met ? _metInk : _unmetInk;
        }
    }
}
