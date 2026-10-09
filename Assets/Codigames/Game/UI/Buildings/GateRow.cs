using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Buildings
{
    // A requirement of the next level (the web's up-row is-gate): its icon, what it asks, a tick or a cross. An unmet
    // one reads in clay on a rosy section, so the plan's open errands stand out.
    public class GateRow : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private Image _rim;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _mark;
        [SerializeField] private Sprite _tick;
        [SerializeField] private Sprite _cross;
        [SerializeField] private Color _fillMet = new Color32(0xf0, 0xd9, 0xae, 0xff);
        [SerializeField] private Color _rimMet = new Color32(0xcf, 0xa8, 0x74, 0xff);
        [SerializeField] private Color _fillUnmet = new Color32(0xf5, 0xd8, 0xcc, 0xff);
        [SerializeField] private Color _rimUnmet = new Color32(0xdb, 0xa5, 0x96, 0xff);
        [SerializeField] private Color _ink = new Color32(0x3b, 0x24, 0x12, 0xff);
        [SerializeField] private Color _clay = new Color32(0xd4, 0x55, 0x3e, 0xff);

        public void Show(Sprite icon, string label, bool met)
        {
            _icon.sprite = icon;
            _label.text = met ? label : "<font-weight=700>" + label + "</font-weight>";
            _label.color = met ? _ink : _clay;
            _mark.sprite = met ? _tick : _cross;
            _fill.color = met ? _fillMet : _fillUnmet;
            _rim.color = met ? _rimMet : _rimUnmet;
        }
    }
}
