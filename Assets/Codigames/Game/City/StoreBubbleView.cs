using DG.Tweening;
using UnityEngine;

namespace Codigames.Game.City
{
    // The collect bubble over a building whose store is ready: parchment with the currency it holds most of, its
    // rim red when the store is full. It pops in and bobs, each building on its own phase.
    public class StoreBubbleView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _rim;
        [SerializeField] private SpriteRenderer _icon;
        [SerializeField] private Color _rimColor = new Color32(0x8a, 0x5a, 0x2b, 0xff);
        [SerializeField] private Color _fullRimColor = new Color32(0xb3, 0x40, 0x2c, 0xff);
        [SerializeField] private float _bob = 0.05f;
        [SerializeField] private float _bobSeconds = 0.8f;
        [SerializeField] private float _popSeconds = 0.25f;

        private Tween _pop;
        private Tween _bobbing;
        private bool _shown;

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
            _bobbing = transform.DOLocalMoveY(tip.y + _bob, _bobSeconds).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                .SetDelay(phase * _bobSeconds);
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
            _bobbing?.Kill();
        }
    }
}
