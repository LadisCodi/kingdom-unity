using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Reveal
{
    // One item in a supplies card: its picture and how many.
    public class SupplyCell : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _count;

        public void Show(Sprite icon, string count)
        {
            _icon.sprite = icon;
            _count.text = count;
        }
    }
}
