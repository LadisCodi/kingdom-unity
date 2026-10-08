using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Codigames.Modules.UI.Widgets
{
    [RequireComponent(typeof(UnityEngine.CanvasGroup))]
    public class Widget : MonoBehaviour
    {
        [SerializeField] private float _showDuration = 0.25f;
        [SerializeField] private float _hideDuration = 0.2f;

        public bool IsSubscribedToEvents { get; private set; }

        protected CanvasGroup CanvasGroup { get; private set; }

        private CancellationTokenSource _animationCTS;
        private Tween _currentTween;

        public void Initialize()
        {
            CanvasGroup = GetComponent<CanvasGroup>();

            InitializeInternal();
        }

        public void Dispose()
        {
            _animationCTS?.Cancel();
            _animationCTS?.Dispose();
            _currentTween?.Kill();
            DisposeInternal();
        }

        public void SubscribeToEvents()
        {
            if (IsSubscribedToEvents) return;
            SubscribeToEventsInternal();
            IsSubscribedToEvents = true;
        }

        public void UnsubscribeFromEvents()
        {
            if (!IsSubscribedToEvents) return;
            UnsubscribeFromEventsInternal();
            IsSubscribedToEvents = false;
        }

        public virtual async UniTask Show(bool immediate = false)
        {
            ResetAnimationToken();
            _currentTween?.Kill();

            if (immediate)
            {
                CanvasGroup.alpha = 1f;
                transform.localScale = Vector3.one;
                CanvasGroup.interactable = true;
                CanvasGroup.blocksRaycasts = true;
                return;
            }

            CanvasGroup.interactable = false;

            _currentTween = DOTween.Sequence()
                .Append(CanvasGroup.DOFade(1f, _showDuration))
                .Join(transform.DOScale(1f, _showDuration).From(0.9f).SetEase(Ease.OutBack))
                .OnComplete(() =>
                {
                    CanvasGroup.interactable = true;
                    CanvasGroup.blocksRaycasts = true;
                });

            await _currentTween.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, _animationCTS.Token);
        }

        public virtual async UniTask Hide(bool immediate = false)
        {
            ResetAnimationToken();
            _currentTween?.Kill();

            if (immediate)
            {
                CanvasGroup.alpha = 0f;
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
                return;
            }

            CanvasGroup.interactable = false;

            _currentTween = DOTween.Sequence()
                .Append(CanvasGroup.DOFade(0f, _hideDuration))
                .Join(transform.DOScale(0.95f, _hideDuration).SetEase(Ease.InQuad));

            await _currentTween.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, _animationCTS.Token);

            CanvasGroup.blocksRaycasts = false;
        }

        private void ResetAnimationToken()
        {
            _animationCTS?.Cancel();
            _animationCTS?.Dispose();
            _animationCTS = new CancellationTokenSource();
        }

        protected virtual void InitializeInternal() { }
        protected virtual void DisposeInternal() { }
        protected virtual void SubscribeToEventsInternal() { }
        protected virtual void UnsubscribeFromEventsInternal() { }

        #region Unity Lifecycle

        protected virtual void Awake() => Initialize();

        protected virtual void OnEnable() => SubscribeToEvents();

        protected virtual void OnDisable() => UnsubscribeFromEvents();

        protected virtual void OnDestroy() => Dispose();

        #endregion
    }
}