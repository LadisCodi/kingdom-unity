using UnityEngine;

namespace Codigames.Game.City
{
    // One building on the map: its art at its level, standing on its plot; faint with a bar while it is built.
    public class DistrictView : MonoBehaviour
    {
        private const float UNDER_CONSTRUCTION_ALPHA = 0.55f;

        [SerializeField] private SpriteRenderer _art;
        [SerializeField] private ProgressBarView _bar;

        public void Show(Sprite sprite, Vector3 basePosition, float plotWidth, bool built)
        {
            transform.position = basePosition;
            _art.sprite = sprite;

            if (sprite != null)
            {
                var spriteWidth = sprite.rect.width / sprite.pixelsPerUnit;
                _art.transform.localScale = Vector3.one * (plotWidth / spriteWidth);
            }

            _art.color = new Color(1f, 1f, 1f, built ? 1f : UNDER_CONSTRUCTION_ALPHA);
            _bar.SetWidth(plotWidth * 0.8f);
            _bar.gameObject.SetActive(!built);
        }

        public void SetProgress(float fraction) => _bar.SetFraction(fraction);
    }
}
