using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Codigames.Game.UI.Widgets
{
    [RequireComponent(typeof(UnityEngine.CanvasGroup))]
    public abstract class StateView<TData, TState> : MonoBehaviour, IDisposable where TState : Enum
    {
        public abstract TState StateId { get; }
        public abstract bool IsConditionMet(TData data);

        protected CanvasGroup CanvasGroup { get; private set; }
        public bool IsSubscribedToEvents { get; private set; }

        private CancellationTokenSource _animationCTS;
        private IStateViewAnimation _animation;
        private Tween _currentTween;

        public virtual void Initialize()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            _animation = GetComponent<IStateViewAnimation>();

            // Without an animation, start hidden so the first Enter is consistent.
            if (_animation == null)
            {
                CanvasGroup.alpha = 0;
            }

            InitializeInternal();
        }

        public virtual async UniTask Enter(TData data)
        {
            ResetAnimationToken();

            gameObject.SetActive(true);
            Refresh(data);
            SubscribeToEvents();

            if (_animation != null)
            {
                await _animation.PlayShow(_animationCTS.Token);
            }
            else
            {
                CanvasGroup.alpha = 1;
                CanvasGroup.interactable = true;
                CanvasGroup.blocksRaycasts = true;
            }
        }

        public virtual async UniTask Exit()
        {
            ResetAnimationToken();
            UnSubscribeFromEvents();

            if (_animation != null)
            {
                await _animation.PlayHide(_animationCTS.Token);
            }
            else
            {
                CanvasGroup.alpha = 0;
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void ResetAnimationToken()
        {
            _animationCTS?.Cancel();
            _animationCTS?.Dispose();
            _animationCTS = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _currentTween?.Kill();
        }

        public abstract void Refresh(TData data);

        private void SubscribeToEvents()
        {
            if (IsSubscribedToEvents) return;
            SubscribeToEventsInternal();
            IsSubscribedToEvents = true;
        }

        private void UnSubscribeFromEvents()
        {
            if (!IsSubscribedToEvents) return;
            UnSubscribeFromEventsInternal();
            IsSubscribedToEvents = false;
        }

        protected virtual void SubscribeToEventsInternal() { }
        protected virtual void UnSubscribeFromEventsInternal() { }
        protected virtual void InitializeInternal() { }
        protected virtual void DisposeInternal() { }

        protected virtual void OnDestroy() => Dispose();

        public void Dispose()
        {
            _animationCTS?.Cancel();
            _animationCTS?.Dispose();
            _currentTween?.Kill();

            _animationCTS = null;
            _currentTween = null;

            UnSubscribeFromEvents();
            DisposeInternal();
        }
    }
}