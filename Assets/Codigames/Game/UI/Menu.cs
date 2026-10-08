using Codigames.Game.UI.Widgets;
using System.Threading.Tasks;
using Codigames.Modules.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.UI
{
    [RequireComponent(typeof(UnityEngine.CanvasGroup))]
    public abstract class Menu : MonoBehaviour, IMenuView
    {
        private static readonly int VISIBILITY_PARAMETER = Animator.StringToHash("IsVisible");
        private const string SHOW_NODE = "Show";
        private const string HIDE_NODE = "Hide";

        [Title("Basic Behaviour")]
        [SerializeField] private bool _hideOnStart = true;

        [Header("Animations")]
        [SerializeField, ReadOnly] private bool _hasAnimator;
        [SerializeField, ReadOnly] private float _showAnimationDuration = 0.5f;
        [SerializeField, ReadOnly] private float _hideAnimationDuration = 0.5f;

        public bool IsSubscribedToEvents { get; private set; }

        protected CanvasGroup CanvasGroup { get; private set; }
        private Animator _animator;

        public void Initialize()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            _animator = GetComponent<Animator>();

            if (_hideOnStart)
            {
                CanvasGroup.alpha = 0f;
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
            }

            if (_animator != null)
            {
                _hasAnimator = true;
            }

            InitializeInternal();
        }

        protected virtual void InitializeInternal()
        {

        }

        public void Dispose()
        {
            DisposeInternal();
        }

        protected virtual void DisposeInternal()
        {

        }

        private void SubscribeToEvents()
        {
            if (IsSubscribedToEvents) return;

            var widgets = GetComponentsInChildren<Widget>(false);

            foreach (var widget in widgets)
            {
                widget.SubscribeToEvents();
            }

            SubscribeToEventsInternal();

            IsSubscribedToEvents = true;
        }

        protected virtual void SubscribeToEventsInternal()
        {

        }

        private void UnsubscribeFromEvents()
        {
            if (IsSubscribedToEvents == false) return;

            var widgets = GetComponentsInChildren<Widget>(false);

            foreach (var widget in widgets)
            {
                widget.UnsubscribeFromEvents();
            }

            UnsubscribeFromEventsInternal();

            IsSubscribedToEvents = false;
        }

        protected virtual void UnsubscribeFromEventsInternal()
        {

        }

        public virtual async Task Show()
        {
            gameObject.SetActive(true);

            PreShow();
            await PlayShowAnimation();
            PostShow();

            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
        }

        public virtual async Task Hide()
        {
            PreHide();
            await PlayHideAnimation();
            PostHide();

            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        // Called when this menu becomes / stops being the top-most menu in the UI stack.
        public virtual void OnFocusGained() { }
        public virtual void OnFocusLost() { }

        private void PreShow()
        {
            PreShowInternal();
        }

        private void PostShow()
        {
            SubscribeToEvents();
            PostShowInternal();
        }

        private void PreHide()
        {
            UnsubscribeFromEvents();
            PreHideInternal();
        }

        private void PostHide()
        {
            PostHideInternal();
        }

        protected virtual void PreShowInternal() { }
        protected virtual void PostShowInternal() { }
        protected virtual void PreHideInternal() { }
        protected virtual void PostHideInternal() { }

        protected virtual async UniTask PlayShowAnimation()
        {
            var ct = this.GetCancellationTokenOnDestroy();

            if (_hasAnimator && HasAnimationParameter(VISIBILITY_PARAMETER))
            {
                _animator.SetBool(VISIBILITY_PARAMETER, true);
                await UniTask.Delay(System.TimeSpan.FromSeconds(_showAnimationDuration), cancellationToken: ct);
            }
            else
            {
                await CanvasGroup
                    .DOFade(1f, _showAnimationDuration)
                    .SetEase(Ease.OutQuad)
                    .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken: ct);
            }
        }

        protected virtual async UniTask PlayHideAnimation()
        {
            var ct = this.GetCancellationTokenOnDestroy();

            if (_hasAnimator && HasAnimationParameter(VISIBILITY_PARAMETER))
            {
                _animator.SetBool(VISIBILITY_PARAMETER, false);
                await UniTask.Delay(System.TimeSpan.FromSeconds(_hideAnimationDuration), cancellationToken: ct);
            }
            else
            {
                await CanvasGroup
                    .DOFade(0f, _hideAnimationDuration)
                    .SetEase(Ease.OutQuad)
                    .ToUniTask(TweenCancelBehaviour.KillWithCompleteCallback, cancellationToken: ct);
            }
        }

        private bool HasAnimationParameter(int paramHash)
        {
            if (_animator == null) return false;

            foreach (var param in _animator.parameters)
            {
                if (param.nameHash == paramHash) return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _animator = GetComponent<Animator>();
            _hasAnimator = _animator != null;

            if (_hasAnimator && _animator.runtimeAnimatorController is UnityEditor.Animations.AnimatorController controller)
            {
                _showAnimationDuration = GetEditorAnimationDuration(controller, SHOW_NODE);
                _hideAnimationDuration = GetEditorAnimationDuration(controller, HIDE_NODE);
            }
        }

        private float GetEditorAnimationDuration(UnityEditor.Animations.AnimatorController controller, string nodeName)
        {
            foreach (var layer in controller.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.name == nodeName && state.state.motion != null)
                    {
                        return state.state.motion.averageDuration;
                    }
                }
            }
            return 0.5f;
        }
#endif
    }
}
