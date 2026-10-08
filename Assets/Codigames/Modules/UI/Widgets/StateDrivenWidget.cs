using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Modules.UI.Widgets
{
    public abstract class StateDrivenWidget<TData, TState> : Widget where TState : Enum
    {
        [Header("Transition Settings")]
        [SerializeField] private TransitionMode _transitionMode = TransitionMode.CrossFade;

        [Title("Editor Tools")]
        [EnumPaging]
        [OnValueChanged(nameof(ApplyEditorPreview))]
        [SerializeField] private TState _editorPreviewState;

        private List<StateView<TData, TState>> _orderedViews;
        private StateView<TData, TState> _activeView;
        private bool _isTransitioning;
        private bool _pendingEvaluation;

        public TData Data { get; private set; }
        public TState CurrentState => _activeView != null ? _activeView.StateId : default;

        // The state view currently shown, so subclasses can drive view-specific behaviour (e.g. feedback).
        protected StateView<TData, TState> ActiveView => _activeView;

        protected override void InitializeInternal()
        {
            // Idempotent: SetData may lazily init before Awake runs (see SetData), so a later Awake mustn't
            // re-gather and re-deactivate an already-entered view.
            if (_orderedViews != null) return;

            _orderedViews = new List<StateView<TData, TState>>(GetComponentsInChildren<StateView<TData, TState>>(true));

            foreach (var view in _orderedViews)
            {
                view.Initialize();
                view.gameObject.SetActive(false);
            }
        }

        protected override void DisposeInternal()
        {
            if (_orderedViews == null) return;

            foreach (var view in _orderedViews)
            {
                view.Dispose();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Re-show the current state view when the widget becomes active again. While hidden it may
            // have been left set but not entered (deactivated / alpha 0) — e.g. a state change that
            // happened while the widget was unsubscribed.
            if (_activeView != null && !_activeView.gameObject.activeSelf)
            {
                _activeView.Enter(Data).Forget();
            }
        }

        #region State Logic

        public void SetData(TData data)
        {
            // A widget bound while still inactive in the hierarchy (e.g. a world-space widget under an inactive
            // parent) won't have run Awake yet, so ensure the views are gathered before evaluating.
            if (_orderedViews == null) InitializeInternal();

            Data = data;
            EvaluateAndTransition(data);
        }

        private void EvaluateAndTransition(TData data)
        {
            var viewToActivate = FindView(data);

            if (viewToActivate == null)
            {
                Debug.LogError($"#UI# Widget {name} has no state view for its data.");
                return;
            }

            if (_activeView == viewToActivate)
            {
                _activeView.Refresh(data);
                return;
            }

            TransitionTo(viewToActivate).Forget();
        }

        private StateView<TData, TState> FindView(TData data)
        {
            foreach (var view in _orderedViews)
            {
                if (view.IsConditionMet(data)) return view;
            }

            return null;
        }

        // A state change that arrives mid-transition is applied once the transition ends, so the widget
        // always settles on the latest data.
        private async UniTask TransitionTo(StateView<TData, TState> nextView)
        {
            if (_isTransitioning)
            {
                _pendingEvaluation = true;
                return;
            }

            _isTransitioning = true;

            var previousView = _activeView;
            _activeView = nextView;

            if (_transitionMode == TransitionMode.Sequential)
            {
                if (previousView != null) await previousView.Exit();
                if (IsSubscribedToEvents) await _activeView.Enter(Data);
            }
            else
            {
                if (previousView != null) previousView.Exit().Forget();
                if (IsSubscribedToEvents) await _activeView.Enter(Data);
            }

            _isTransitioning = false;

            if (_pendingEvaluation)
            {
                _pendingEvaluation = false;
                EvaluateAndTransition(Data);
            }
        }

        #endregion

        #region Editor Logic (Odin)

        [Button(ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
        private void RefreshViewsList()
        {
            _orderedViews = new List<StateView<TData, TState>>(GetComponentsInChildren<StateView<TData, TState>>(true));
            ApplyEditorPreview();
        }

        private void ApplyEditorPreview()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;

            var views = GetComponentsInChildren<StateView<TData, TState>>(true);

            foreach (var view in views)
            {
                bool shouldBeActive = view.StateId.Equals(_editorPreviewState);

                UnityEditor.Undo.RecordObject(view.gameObject, "State Preview");

                view.gameObject.SetActive(shouldBeActive);

                var group = view.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = shouldBeActive ? 1 : 0;
                }
            }
#endif
        }

        #endregion
    }
}