using System;
using System.Threading.Tasks;

namespace Codigames.Modules.UI
{
    // The shared lifecycle of a presenter: resolve the view, bind it, wire its events, show it; and the same
    // backwards to hide it. A presenter overrides the hooks, never Show or Hide.
    public abstract class AbstractMenuPresenter<TView> : IMenuPresenter where TView : class, IMenuView
    {
        private readonly IMenuViewFactory _views;
        private bool _viewEventsSubscribed;

        protected AbstractMenuPresenter(IMenuViewFactory views)
        {
            _views = views;
        }

        public Type MenuType => typeof(TView);
        public bool IsShown { get; private set; }

        // Bumped by every show and hide: a show that a later hide overtook (both animating at once) must not leave the
        // menu counted as shown with its view put away.
        private int _turn;
        public bool HasFocus { get; private set; }

        protected TView View { get; private set; }

        public async Task Show()
        {
            View ??= _views.Resolve<TView>();
            var turn = ++_turn;

            BindInternal(View);
            SubscribeToViewEvents(View);
            await PreShowInternal(View);
            await View.Show();
            await PostShowInternal(View);

            if (turn == _turn) IsShown = true;
        }

        public async Task Hide()
        {
            if (View == null) return;
            _turn++;

            await PreHideInternal(View);
            await View.Hide();
            await PostHideInternal(View);
            UnsubscribeFromViewEvents(View);
            UnbindInternal(View);

            IsShown = false;
        }

        public void OnFocusGained()
        {
            HasFocus = true;
            View?.OnFocusGained();
            OnFocusGainedInternal();
        }

        public void OnFocusLost()
        {
            HasFocus = false;
            View?.OnFocusLost();
            OnFocusLostInternal();
        }

        // Show can run again while the menu is shown; unsubscribing first keeps handlers from stacking.
        private void SubscribeToViewEvents(TView view)
        {
            if (_viewEventsSubscribed) UnsubscribeFromViewEventsInternal(view);

            SubscribeToViewEventsInternal(view);
            _viewEventsSubscribed = true;
        }

        private void UnsubscribeFromViewEvents(TView view)
        {
            if (!_viewEventsSubscribed) return;

            UnsubscribeFromViewEventsInternal(view);
            _viewEventsSubscribed = false;
        }

        // Set the view's references and push its initial state (no event wiring).
        protected virtual void BindInternal(TView view) { }
        protected virtual void UnbindInternal(TView view) { }

        // Subscribe to / unsubscribe from the view's events.
        protected virtual void SubscribeToViewEventsInternal(TView view) { }
        protected virtual void UnsubscribeFromViewEventsInternal(TView view) { }

        // Becoming / stopping being the top-most menu (the view is already told).
        protected virtual void OnFocusGainedInternal() { }
        protected virtual void OnFocusLostInternal() { }

        protected virtual Task PreShowInternal(TView view) => Task.CompletedTask;
        protected virtual Task PostShowInternal(TView view) => Task.CompletedTask;
        protected virtual Task PreHideInternal(TView view) => Task.CompletedTask;
        protected virtual Task PostHideInternal(TView view) => Task.CompletedTask;
    }
}
