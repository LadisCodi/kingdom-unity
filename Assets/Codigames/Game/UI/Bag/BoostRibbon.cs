using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Bag
{
    // A boost running, over the Boosts tab's grid (the web's bag-ribbon): green cloth, its icon, what it raises, and
    // the time left at its end.
    public class BoostRibbon : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _what;
        [SerializeField] private TMP_Text _left;

        public void Show(BoostRibbonData ribbon)
        {
            _icon.sprite = ribbon.Icon;
            _what.text = ribbon.What;
            _left.text = ribbon.Left;
        }
    }
}
