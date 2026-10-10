using DG.Tweening;
using UnityEngine;

namespace Codigames.Game.City
{
    // The collect bubble over a building whose store is ready: parchment with the currency it holds most of, its
    // rim red when the store is full. It pops in and bobs, each building on its own phase, and gives a small hop when
    // a haul lands in the store (the web's collectBubbles.ts: sin over 260 ms, 0.22 of its width).
    public class StoreBubbleView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _rim;
        [SerializeField] private SpriteRenderer _icon;
        [SerializeField] private Color _rimColor = new Color32(0x8a, 0x5a, 0x2b, 0xff);
        [SerializeField] private Color _fullRimColor = new Color32(0xb3, 0x40, 0x2c, 0xff);
        [SerializeField] private float _bob = 0.05f;
        [SerializeField] private float _bobSeconds = 0.8f;
        [SerializeField] private float _popSeconds = 0.25f;
        [SerializeField] private float _hopSeconds = 0.26f;
        [SerializeField, Tooltip("How high a hop goes, in widths of the bubble.")] private float _hop = 0.22f;

        private Tween _pop;
        private bool _shown;
        private Vector3 _tip;
        private float _bobFrom;
        private float _hopAt = float.NegativeInfinity;

        public void Show(Sprite icon, bool full, Vector3 tip, float phase)
        {
            _icon.sprite = icon;
            _rim.color = full ? _fullRimColor : _rimColor;
            if (_shown) return;
            transform.localPosition = tip;
            _shown = true;
            gameObject.SetActive(true);

            Stop();
            transform.localScale = Vector3.zero;
            _pop = transform.DOScale(1f, _popSeconds).SetEase(Ease.OutBack);
            _tip = tip;
            _bobFrom = Time.time + phase * _bobSeconds;
            _hopAt = float.NegativeInfinity;
        }

        // A haul landed in the store.
        public void Hop()
        {
            if (_shown) _hopAt = Time.time;
        }

        // Up and down between the tip and a little above it, eased at both ends; a hop on top.
        private void Update()
        {
            var since = Mathf.Max(0f, Time.time - _bobFrom) / _bobSeconds;
            var y = _tip.y + _bob * (1f - Mathf.Cos(Mathf.PI * since)) / 2f;
            var k = (Time.time - _hopAt) / _hopSeconds;
            if (k is >= 0f and < 1f && _rim.sprite != null)
                y += Mathf.Sin(k * Mathf.PI) * _rim.sprite.bounds.size.x * _rim.transform.localScale.x * _hop;
            transform.localPosition = new Vector3(_tip.x, y, _tip.z);
        }

        public void Hide()
        {
            if (!_shown) return;
            _shown = false;
            Stop();
            gameObject.SetActive(false);
        }

        private void OnDestroy() => Stop();

        private void Stop()
        {
            _pop?.Kill();
        }
    }
}
