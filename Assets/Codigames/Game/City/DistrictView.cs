using UnityEngine;

namespace Codigames.Game.City
{
    // One building on the map: its art at its level, standing on its plot; faint with a bar while it is built.
    public class DistrictView : MonoBehaviour
    {
        private const float UNDER_CONSTRUCTION_ALPHA = 0.55f;
        private const float BAR_HEIGHT = 0.2f;
        private const float BUBBLE_HEIGHT = 0.6f;

        private float _plotWidth = 1f;
        private Vector3 _artScale = Vector3.one;

        [SerializeField] private SpriteRenderer _art;
        [SerializeField] private ProgressBarView _bar;
        [SerializeField] private StoreBubbleView _bubble;

        public void Show(Sprite sprite, Vector3 basePosition, float plotWidth, bool built)
        {
            transform.position = basePosition;
            _plotWidth = plotWidth;
            _art.sprite = sprite;

            if (sprite != null)
            {
                var spriteWidth = sprite.rect.width / sprite.pixelsPerUnit;
                _artScale = Vector3.one * (plotWidth / spriteWidth);
                _art.transform.localScale = _artScale;
            }

            _art.color = new Color(1f, 1f, 1f, built ? 1f : UNDER_CONSTRUCTION_ALPHA);

            // On the plot's middle, not over the art: the bar belongs to the ground being worked.
            _bar.transform.localPosition = new Vector3(0f, plotWidth / 4f, 0f);
            _bar.SetSize(Mathf.Max(BAR_HEIGHT * 4f, plotWidth * 0.6f), BAR_HEIGHT);
            _bar.gameObject.SetActive(!built);
        }

        // A tap's squash and stretch, about the building's feet.
        public void SetPunch(Vector2 scale) => _art.transform.localScale = new Vector3(_artScale.x * scale.x, _artScale.y * scale.y, 1f);

        // Over the roof while the store is ready to collect.
        public void SetStore(bool ready, Sprite icon, bool full)
        {
            if (!ready)
            {
                _bubble.Hide();
                return;
            }

            // Just over the roof: a building stands about as tall as its plot is wide, half of it plot.
            _bubble.Show(icon, full, new Vector3(0f, _plotWidth * BUBBLE_HEIGHT, 0f),
                Mathf.Repeat(transform.position.x * 3.7f + transform.position.y * 1.3f, 1f));
        }

        public void SetProgress(float fraction, string remaining)
        {
            _bar.SetFraction(fraction);
            _bar.SetLabel(remaining);
        }
    }
}
