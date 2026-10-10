using UnityEngine;

namespace Codigames.Game.City
{
    // One building on the map: its art at its level, standing on its plot; faint with a bar while it is built.
    public class DistrictView : MonoBehaviour
    {
        private const float UNDER_CONSTRUCTION_ALPHA = 0.55f;
        // Faint at its old address while its ghost is out.
        private const float LIFTED_ALPHA = 0.28f;
        private const float BAR_HEIGHT = 0.2f;
        private const float BUBBLE_HEIGHT = 0.6f;
        // The hammer's frame: at most this many cells wide, its top this far up the art.
        private const float HAMMER_CELLS = 1.6f;
        private const float HAMMER_TOP = 0.85f;
        private const float FLASH_IN_FRONT = 0.002f;

        private float _plotWidth = 1f;
        private Vector3 _artScale = Vector3.one;
        private SpriteRenderer _flash;
        private float _alpha = 1f;
        private bool _lifted;

        [SerializeField] private SpriteRenderer _art;
        [SerializeField] private ProgressBarView _bar;
        [SerializeField] private StoreBubbleView _bubble;
        [SerializeField] private WorkingHammer _hammer;

        // The drawing's box in the world: what stands over the roof sits on it.
        public Bounds ArtBounds => _art.bounds;

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

            _alpha = built ? 1f : UNDER_CONSTRUCTION_ALPHA;
            Fade();

            // A builder at work hammers over the art's upper half.
            var tall = sprite != null ? sprite.rect.height / sprite.pixelsPerUnit * _artScale.y : plotWidth;
            var hammerWidth = Mathf.Min(plotWidth, HAMMER_CELLS);
            _hammer.Place(new Vector3(-hammerWidth / 2f, tall * HAMMER_TOP, 0f), hammerWidth, name);

            // On the plot's middle, not over the art: the bar belongs to the ground being worked.
            _bar.transform.localPosition = new Vector3(0f, plotWidth / 4f, 0f);
            _bar.SetSize(Mathf.Max(BAR_HEIGHT * 4f, plotWidth * 0.6f), BAR_HEIGHT);
            _bar.gameObject.SetActive(!built);
        }

        // Picked up to be moved: faint where it stands until it is put down.
        public void SetLifted(bool lifted)
        {
            _lifted = lifted;
            Fade();
        }

        private void Fade() => _art.color = new Color(1f, 1f, 1f, _alpha * (_lifted ? LIFTED_ALPHA : 1f));

        // A builder is at work on it: a build or an upgrade.
        public void SetWorking(bool working) => _hammer.gameObject.SetActive(working);

        // A tap's squash and stretch, about the building's feet.
        public void SetPunch(Vector2 scale) => _art.transform.localScale = new Vector3(_artScale.x * scale.x, _artScale.y * scale.y, 1f);

        // Planted from the ghost: the art `lift` plot widths above its plot, squashed by `scale` about its foot.
        public void SetLanding(float lift, Vector2 scale)
        {
            _art.transform.localPosition = new Vector3(0f, lift * _plotWidth, 0f);
            SetPunch(scale);
        }

        // A tap's white flash: the art added onto itself, just in front of it, while `flash` lasts.
        public void SetFlash(float flash, Material material)
        {
            if (flash <= 0.004f || material == null)
            {
                if (_flash != null) _flash.enabled = false;
                return;
            }

            if (_flash == null)
            {
                _flash = new GameObject("Flash").AddComponent<SpriteRenderer>();
                _flash.transform.SetParent(_art.transform, false);
                _flash.sortingLayerID = _art.sortingLayerID;
                _flash.sortingOrder = _art.sortingOrder;
                _flash.spriteSortPoint = _art.spriteSortPoint;
            }

            // A hair lower on the sort axis than the art, so it draws just after it.
            _flash.transform.localPosition = new Vector3(0f, -FLASH_IN_FRONT / Mathf.Max(0.0001f, _artScale.y), 0f);
            _flash.sharedMaterial = material;
            _flash.sprite = _art.sprite;
            _flash.flipX = _art.flipX;
            _flash.color = new Color(1f, 1f, 1f, flash * _art.color.a);
            _flash.enabled = true;
        }

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

        // A hall's training over it; null when its line is empty.
        public void SetTraining(float? fraction, string remaining)
        {
            if (fraction == null)
            {
                if (_training) _bar.gameObject.SetActive(false);
                _training = false;
                return;
            }

            _training = true;
            _bar.gameObject.SetActive(true);
            _bar.SetFraction(fraction.Value);
            _bar.SetLabel(remaining);
        }

        private bool _training;

        public void SetProgress(float fraction, string remaining)
        {
            _bar.SetFraction(fraction);
            _bar.SetLabel(remaining);
        }
    }
}
