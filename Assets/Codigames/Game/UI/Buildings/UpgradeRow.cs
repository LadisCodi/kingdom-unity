using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Buildings
{
    // A figure the next level moves (the web's up-row): its icon and name, its value now, and what the level adds —
    // green, or clay when the level makes it worse.
    public class UpgradeRow : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private TMP_Text _delta;
        [SerializeField] private Color _better = new Color32(0x3f, 0x8a, 0x2e, 0xff);
        [SerializeField] private Color _worse = new Color32(0xd4, 0x55, 0x3e, 0xff);

        public void Show(Sprite icon, string label, string value, string delta, bool worse)
        {
            _icon.sprite = icon;
            _label.text = label;
            _value.text = value;
            _delta.text = delta;
            _delta.color = worse ? _worse : _better;
        }
    }
}
