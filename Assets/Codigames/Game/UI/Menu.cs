using Codigames.Game.UI.Widgets;
using System.Threading.Tasks;
using Codigames.Modules.Audio;
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
        // THE WINDOW'S ENTRANCE AND EXIT (the web's k-window-in / k-window-out, kit.css): a menu built on a window
        // (Safe/Window) opens from its least height to its full height as it fades in, over 160 ms; it closes back
        // to its least height and then fades, over 200 ms. A window docked at the bottom grows up; any other grows
        // from its middle. What pokes out of it (its head plank, its close) is inside the clip's margin.
        private const string WINDOW = "Safe/Window";
        private const float WINDOW_IN = 0.16f;
        private const float WINDOW_OUT = 0.2f;
        private const float WINDOW_MIN = 120f;
        private const float CLIP_MARGIN = 400f;

        [Title("Basic Behaviour")]
        [SerializeField] private bool _hideOnStart = true;

        [Header("Animations")]
        [SerializeField, ReadOnly] private bool _hasAnimator;
        [SerializeField, ReadOnly] private float _showAnimationDuration = 0.5f;
        [SerializeField, ReadOnly] private float _hideAnimationDuration = 0.5f;

        public bool IsSubscribedToEvents { get; private set; }

        protected CanvasGroup CanvasGroup { get; private set; }
        private Animator _animator;
        private RectTransform _unrolled;
        private UnityEngine.UI.RectMask2D _unrolledClip;
        // Which show or hide is the latest: one overtaken leaves the menu to the one that overtook it.
        private int _turn;
        private ISoundService _sounds;

        // The game's sounds, for a view that plays its own sequence (the reveal).
        protected ISoundService Sounds => _sounds;

        [VContainer.Inject]
        public void Construct(ISoundService sounds) => _sounds = sounds;

        public void Initialize()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            _animator = GetComponent<Animator>();
            _unrolled = transform.Find(WINDOW) as RectTransform;

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

            WireClicks(gameObject);
            InitializeInternal();
        }

        // Every button under it clicks when pressed; a menu that adds buttons later wires them too.
        protected void WireClicks(GameObject under)
        {
            foreach (var button in under.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                button.onClick.AddListener(PlayClick);
        }

        private void PlayClick() => _sounds?.Play(Codigames.Game.Audio.SoundIds.BUTTON_PRESS);

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
            var turn = ++_turn;
            // The menu opened last is drawn over the ones opened before it (a popup over its sheet).
            transform.SetAsLastSibling();
            gameObject.SetActive(true);

            PreShow();
            await PlayShowAnimation();
            // Closed again while it opened: the close has the last word.
            if (turn != _turn) return;
            PostShow();

            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
        }

        public virtual async Task Hide()
        {
            var turn = ++_turn;
            PreHide();
            await PlayHideAnimation();
            // Opened again while it closed: it stays open.
            if (turn != _turn) return;
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
            else if (_unrolled != null)
            {
                await Unroll(true, ct);
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
            else if (_unrolled != null)
            {
                await Unroll(false, ct);
            }
            else
            {
                await CanvasGroup
                    .DOFade(0f, _hideAnimationDuration)
                    .SetEase(Ease.OutQuad)
                    .ToUniTask(TweenCancelBehaviour.KillWithCompleteCallback, cancellationToken: ct);
            }
        }

        // The window clipped to a band that grows from its least height to all of it (or back), with the fade.
        private async UniTask Unroll(bool open, System.Threading.CancellationToken ct)
        {
            if (_unrolledClip == null) _unrolledClip = _unrolled.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_unrolled);
            var height = _unrolled.rect.height;
            var shut = Mathf.Max(0f, height - Mathf.Min(height, WINDOW_MIN));
            var docked = Docked();
            var closed = docked ? new Vector4(-CLIP_MARGIN, -CLIP_MARGIN, -CLIP_MARGIN, shut) : new Vector4(-CLIP_MARGIN, shut / 2f, -CLIP_MARGIN, shut / 2f);
            var whole = new Vector4(-CLIP_MARGIN, -CLIP_MARGIN, -CLIP_MARGIN, -CLIP_MARGIN);
            _unrolledClip.enabled = true;
            // Not killing a run still going: its await would end early, and a hide overtaken by a show would then
            // switch the menu off under it. The later run writes last each frame, so it wins.
            var sequence = DOTween.Sequence().SetTarget(this).SetUpdate(true);
            if (open)
            {
                _unrolledClip.padding = closed;
                CanvasGroup.alpha = 0f;
                _ = sequence.Join(DOTween.To(() => _unrolledClip.padding, v => _unrolledClip.padding = v, whole, WINDOW_IN).SetEase(Ease.OutCubic));
                _ = sequence.Join(CanvasGroup.DOFade(1f, WINDOW_IN).SetEase(Ease.OutQuad));
            }
            else
            {
                _unrolledClip.padding = whole;
                _ = sequence.Append(DOTween.To(() => _unrolledClip.padding, v => _unrolledClip.padding = v, closed, WINDOW_OUT * 0.75f).SetEase(Ease.InQuad));
                _ = sequence.Append(CanvasGroup.DOFade(0f, WINDOW_OUT * 0.25f));
            }

            await sequence.ToUniTask(TweenCancelBehaviour.KillWithCompleteCallback, cancellationToken: ct);
            // Open, it clips nothing: the window is itself again.
            if (open && _unrolledClip != null && CanvasGroup.alpha >= 1f) _unrolledClip.enabled = false;
        }

        // Sitting on the bottom of its safe area, as a sheet or a card does.
        private bool Docked()
        {
            if (_unrolled.parent is not RectTransform parent) return false;
            var mine = new Vector3[4];
            var area = new Vector3[4];
            _unrolled.GetWorldCorners(mine);
            parent.GetWorldCorners(area);
            var tall = area[1].y - area[0].y;
            return tall > 0f && mine[0].y - area[0].y < tall * 0.15f;
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
