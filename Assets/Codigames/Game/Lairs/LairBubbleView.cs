using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Codigames.Game.Lairs
{
    // The warning bubble over a standing lair (Docs/features/18-garrisons-and-raids.md §6): the collect bubble's shape
    // in danger red, wider than tall, the creature's head on the left and the time to its next raid on the right. Laid
    // out in its sprites' units, a body 1.6 tall; the view scales it to the map. It pops in and bobs.
    public class LairBubbleView : MonoBehaviour
    {
        private const float H = 1.6f;

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private SpriteRenderer _tail;
        [SerializeField] private SpriteRenderer _medal;
        [SerializeField] private SpriteRenderer _hourglass;
        [SerializeField] private TextMeshPro _countdown;
        [SerializeField, Tooltip("How far the tail's top overlaps the body, in the sprites' units.")] private float _overlap = 0.14f;
        [SerializeField] private float _bobSeconds = 0.8f;
        [SerializeField] private float _popSeconds = 0.25f;

        private bool _hourglassShown = true;
        private Tween _pop;
        private Tween _bobbing;
        private string _text;
        private bool _shown;

        // The bubble's own box in world space: what a tap on it hits.
        public Bounds Bounds => _body.bounds;

        public void Show(Sprite medal, string countdown, Vector3 tip, float scale, float phase)
        {
            _medal.sprite = medal;
            SetText(countdown);
            if (_shown) return;
            _shown = true;
            gameObject.SetActive(true);
            transform.localPosition = tip;
            Stop();
            transform.localScale = Vector3.zero;
            _pop = transform.DOScale(scale, _popSeconds).SetEase(Ease.OutBack);
            _bobbing = transform.DOLocalMoveY(tip.y + 0.14f * H * scale * 0.5f, _bobSeconds).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                .SetDelay(phase * _bobSeconds);
        }

        public void Hide()
        {
            if (!_shown) return;
            _shown = false;
            Stop();
            gameObject.SetActive(false);
        }

        // Another bubble on the same shape: a sleeping Shrine's is parchment, its relic and its Zs, no hourglass.
        public void Restyle(Sprite body, Sprite tail, Color ink, bool hourglass)
        {
            _body.sprite = body;
            _tail.sprite = tail;
            _countdown.color = ink;
            _hourglassShown = hourglass;
            _hourglass.gameObject.SetActive(hourglass);
            _text = null;
        }

        // The web's measures, as shares of the body's height: padding 0.2, the head 0.58, the hourglass 0.38.
        public void SetText(string countdown)
        {
            if (countdown == _text) return;
            _text = countdown;
            _countdown.text = countdown;
            _countdown.ForceMeshUpdate();
            var pad = 0.2f * H;
            var head = 0.58f * H;
            var glass = _hourglassShown ? 0.38f * H : 0;
            var textW = _countdown.preferredWidth;
            var w = Mathf.Max(1.6f * H, pad + head + 0.14f * H + glass + 0.06f * H + textW + pad);
            var tailH = _tail.sprite.bounds.size.y;
            var mid = tailH - _overlap + H / 2;
            _body.size = new Vector2(w, H);
            _body.transform.localPosition = new Vector3(0, tailH - _overlap, 0);
            var x = -w / 2 + pad;
            _medal.transform.localPosition = new Vector3(x + head / 2, mid - head / 2, 0);
            _medal.transform.localScale = Vector3.one * (head / _medal.sprite.bounds.size.x);
            x += head + 0.14f * H;
            _hourglass.transform.localPosition = new Vector3(x + glass / 2, mid, 0);
            _hourglass.transform.localScale = Vector3.one * (glass / _hourglass.sprite.bounds.size.y);
            x += glass + 0.06f * H;
            _countdown.rectTransform.pivot = new Vector2(0, 0.5f);
            _countdown.rectTransform.localPosition = new Vector3(x, mid - 0.02f * H, 0);
        }

        private void OnDestroy() => Stop();

        private void Stop()
        {
            _pop?.Kill();
            _bobbing?.Kill();
        }
    }
}
