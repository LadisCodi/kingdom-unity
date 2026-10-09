using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Kit
{
    // One figure a building is judged on (the web's dc-stat): a tile of darker paper, a big icon, the name in ink
    // over the value in a lighter one — the value in clay when it is bad news (a full store).
    public class StatTile : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private Color _valueColor = new Color32(0x7a, 0x5c, 0x3e, 0xff);
        [SerializeField] private Color _badColor = new Color32(0xd4, 0x55, 0x3e, 0xff);

        public void Show(Sprite icon, string label, string value, bool bad)
        {
            _icon.sprite = icon;
            _label.text = label;
            _value.text = value;
            _value.color = bad ? _badColor : _valueColor;
        }
    }
}
