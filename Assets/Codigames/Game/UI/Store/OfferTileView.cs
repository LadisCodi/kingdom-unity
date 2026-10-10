using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Store
{
    // One reward as a tile (the web's ofs-tile): its picture on the tile and its count in the corner.
    public sealed class OfferTileData
    {
        public Sprite Icon;
        public string Count;
        public bool Hero;
    }

    public class OfferTileView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _count;
        [SerializeField, Tooltip("A hero's tile is gilded.")] private Image _gilt;

        public void Show(OfferTileData data)
        {
            _icon.sprite = data.Icon;
            _icon.enabled = data.Icon != null;
            _count.text = data.Count;
            if (_gilt != null) _gilt.enabled = data.Hero;
        }
    }
}
