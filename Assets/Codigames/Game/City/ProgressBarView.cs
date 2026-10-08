using UnityEngine;

namespace Codigames.Game.City
{
    // A wood-and-glass bar on the map (the web's trough bar): a base, a fill cut to the fraction, a border.
    public class ProgressBarView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _base;
        [SerializeField] private SpriteRenderer _fill;
        [SerializeField] private SpriteRenderer _border;
        [SerializeField] private Vector2 _size = new(0.8f, 0.1f);

        public void SetFraction(float fraction)
        {
            var clamped = Mathf.Clamp01(fraction);
            _base.size = _size;
            _border.size = new Vector2(_size.x + _size.y * 0.12f, _size.y * 1.18f);
            _fill.enabled = clamped > 0f;
            _fill.size = new Vector2(Mathf.Max(_size.y, _size.x * clamped), _size.y);
            _fill.transform.localPosition = new Vector3((_fill.size.x - _size.x) / 2f, 0f, 0f);
        }

        public void SetWidth(float width) => _size = new Vector2(width, _size.y);
    }
}
