using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Kit
{
    // A verdict on a pill (the web's dc-badge): leaf for good, clay for bad, its rim and words in that colour over a
    // faint wash of it.
    public class ToneBadge : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private Image _rim;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Color _good = new Color32(0x3f, 0x8a, 0x2e, 0xff);
        [SerializeField] private Color _goodWash = new Color(111 / 255f, 191 / 255f, 74 / 255f, 0.18f);
        [SerializeField] private Color _bad = new Color32(0xd4, 0x55, 0x3e, 0xff);
        [SerializeField] private Color _badWash = new Color(212 / 255f, 85 / 255f, 62 / 255f, 0.15f);

        public void Show(string text, bool good)
        {
            _text.text = text;
            _text.color = good ? _good : _bad;
            _rim.color = good ? _good : _bad;
            _fill.color = good ? _goodWash : _badWash;
        }
    }
}
