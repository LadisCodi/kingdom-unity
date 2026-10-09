using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Kit
{
    // A building at a level, in a tile of darker paper with an ornament pressed into each corner (the web's
    // dc-portrait). A map sprite is a tall canvas with the building in its lower part, so the point 68% of the way
    // down goes to the tile's middle, and the building is drawn a quarter wider than the tile — the mask trims it.
    public class BuildingPortrait : MonoBehaviour
    {
        private const float OVERSIZE = 1.25f;
        private const float MIDDLE_FROM_TOP = 0.68f;

        [SerializeField] private RectTransform _mask;
        [SerializeField] private Image _art;
        [SerializeField, Tooltip("Over the portrait while the building is being built; optional.")] private PortraitHammer _hammer;

        public void Show(Sprite art)
        {
            _art.sprite = art;
            _art.enabled = art != null;
            Fit();
        }

        // The hammer works over it while `working` names the building being built; null rests it.
        public void SetWorking(string working)
        {
            if (_hammer == null) return;
            if (working == null) _hammer.Rest();
            else _hammer.Work(working);
        }

        private void OnRectTransformDimensionsChange() => Fit();

        private void Fit()
        {
            if (_art == null || _art.sprite == null || _mask == null) return;
            var rect = _art.rectTransform;
            var width = _mask.rect.width * OVERSIZE;
            var size = _art.sprite.rect.size;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f - MIDDLE_FROM_TOP);
            rect.sizeDelta = new Vector2(width, width * size.y / size.x);
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
