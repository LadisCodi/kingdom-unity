using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Buildings
{
    // One number a soldier is chosen on (the web's tr-stat): a small section, the mark and the figure, the name under.
    public class TroopStatTile : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private TMP_Text _label;

        public void Show(Data.StatTileData stat)
        {
            _icon.sprite = stat.Icon;
            _value.text = stat.Value;
            _label.text = stat.Label;
        }
    }
}
