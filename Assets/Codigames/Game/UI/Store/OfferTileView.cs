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
        private const string SMALL_COUNT = "Count";

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

        // A panel of many rewards draws them smaller (offer.css .ofs-panel.is-many): the tile and its count.
        public void Shrink(float size)
        {
            var element = GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = element.minHeight = element.preferredHeight = size;
            if (TMP_Settings.defaultStyleSheet != null && TMP_Settings.defaultStyleSheet.GetStyle(SMALL_COUNT) is { } style) _count.textStyle = style;
        }
    }
}
