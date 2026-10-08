using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

namespace Codigames.Modules.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ButtonPressScaleFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private ButtonFeedbackSettings _settings;

        private RectTransform _rect;
        private Vector3 _originalScale;
        private Tween _currentTween;
        private IUISoundPlayer _sound;
        private Selectable _selectable;

        [Inject]
        public void Construct(IUISoundPlayer sound)
        {
            _sound = sound;
        }

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _originalScale = _rect.localScale;
            _selectable = GetComponent<Selectable>();
        }

        // A disabled Selectable (e.g. a non-interactable Button) still receives pointer events, so skip the
        // press feedback when it isn't interactable.
        private bool IsInteractable() => _selectable == null || _selectable.IsInteractable();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable()) return;

            AnimateTo(_originalScale * _settings.PressedScale, _settings.PressDuration, _settings.PressEase);

            if (!string.IsNullOrEmpty(_settings.PressSound)) _sound?.Play(_settings.PressSound, _settings.SoundVolume);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateTo(_originalScale, _settings.ReleaseDuration, _settings.ReleaseEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateTo(_originalScale, _settings.ReleaseDuration, _settings.ReleaseEase);
        }

        private void AnimateTo(Vector3 target, float duration, Ease ease)
        {
            _currentTween?.Kill();
            _currentTween = _rect.DOScale(target, duration).SetEase(ease);
        }

        private void OnDestroy()
        {
            _currentTween?.Kill();
        }
    }
}