using Codigames.Game.Map;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Codigames.Game.UI.Hud
{
    // THE HOLD RING: a long press on something that moves fills a little brass ring beside the finger — up and to the
    // right, where the finger does not cover it — and is gone when the press lets go, wanders or picks it up.
    public class HoldRing : MonoBehaviour
    {
        // Beside the finger: right and up, in reference pixels.
        private static readonly Vector2 BESIDE = new(102f, 138f);
        private const float SHOW_SECONDS = 0.06f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _ring;
        [SerializeField] private Image _fill;

        private MapGestures _gestures;
        private Tween _filling;

        [Inject]
        public void Construct(MapGestures gestures) => _gestures = gestures;

        private void Start()
        {
            _group.alpha = 0;
            _gestures.HoldRingStarted += OnStarted;
            _gestures.HoldRingEnded += OnEnded;
        }

        private void OnDestroy()
        {
            _filling?.Kill();
            if (_gestures == null) return;
            _gestures.HoldRingStarted -= OnStarted;
            _gestures.HoldRingEnded -= OnEnded;
        }

        private void OnStarted(Vector2 screen, float seconds)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_ring.parent, screen, null, out var local);
            _ring.anchoredPosition = local + BESIDE;
            _filling?.Kill();
            _fill.fillAmount = 0;
            _group.DOFade(1, SHOW_SECONDS).SetUpdate(true);
            _filling = _fill.DOFillAmount(1, seconds).SetEase(Ease.Linear).SetUpdate(true);
        }

        private void OnEnded()
        {
            _filling?.Kill();
            _group.alpha = 0;
        }
    }
}
