using UnityEngine;

namespace Codigames.Game.City
{
    // One building on the map: its art at its level, standing on its plot; faint with a bar while it is built.
    public class DistrictView : MonoBehaviour
    {
        private const float UNDER_CONSTRUCTION_ALPHA = 0.55f;
        private const float BAR_HEIGHT = 0.2f;

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

            // On the plot's middle, not over the art: the bar belongs to the ground being worked.
            _bar.transform.localPosition = new Vector3(0f, plotWidth / 4f, 0f);
            _bar.SetSize(Mathf.Max(BAR_HEIGHT * 4f, plotWidth * 0.6f), BAR_HEIGHT);
            _bar.gameObject.SetActive(!built);
        }

        public void SetProgress(float fraction, string remaining)
        {
            _bar.SetFraction(fraction);
            _bar.SetLabel(remaining);
        }
    }
}
