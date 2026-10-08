using System;
using Cysharp.Threading.Tasks;
using VContainer;

namespace Kingdom.Game.UI
{
    public abstract class AbstractMenuPresenter<TMenu> : IMenuPresenter
        where TMenu : Menu
    {
        [Inject] private readonly MenuFactory _menuFactory;

        protected TMenu Menu { get; private set; }

        public Type MenuType => typeof(TMenu);
        public bool IsShown { get; private set; }

        public bool HasFocus { get; private set; }

        private bool _menuEventsSubscribed;

        public async UniTask Show()
        {
            Menu ??= _menuFactory.Resolve<TMenu>();

            Bind(Menu);
            SubscribeToMenuEvents(Menu);
            await PreShowInternal(Menu);
            await Menu.Show();
            await PostShowInternal(Menu);

            IsShown = true;
        }

        public async UniTask Hide()
        {
            if (Menu == null) return;

            await PreHideInternal(Menu);
            await Menu.Hide();
            await PostHideInternal(Menu);
            UnsubscribeFromMenuEvents(Menu);
            Unbind(Menu);

            IsShown = false;
        }

        public void OnFocusGained()
        {
            HasFocus = true;
            Menu?.OnFocusGained();
            OnFocusGainedInternal();
        }

        public void OnFocusLost()
        {
            HasFocus = false;
            Menu?.OnFocusLost();
            OnFocusLostInternal();
        }

        private void Bind(TMenu menu) => BindInternal(menu);
        private void Unbind(TMenu menu) => UnbindInternal(menu);

        // The single, explicit place where the presenter wires up the view's events. Common subscriptions
        // (e.g. a shared close button) live here; per-menu ones in the override. Show() can run again while
        // the menu is already shown (e.g. re-showing an open menu), so if we're already subscribed we
        // unsubscribe first — this keeps handlers from stacking without each presenter guarding it.
        private void SubscribeToMenuEvents(TMenu menu)
        {
            if (_menuEventsSubscribed) UnsubscribeFromMenuEventsInternal(menu);

            SubscribeToMenuEventsInternal(menu);
            _menuEventsSubscribed = true;
        }

        private void UnsubscribeFromMenuEvents(TMenu menu)
        {
            if (!_menuEventsSubscribed) return;

            UnsubscribeFromMenuEventsInternal(menu);
            _menuEventsSubscribed = false;
        }

        // Set view references and push its initial state here (no event wiring).
        protected virtual void BindInternal(TMenu menu) { }
        protected virtual void UnbindInternal(TMenu menu) { }

        // Subscribe/unsubscribe to the view's events here.
        protected virtual void SubscribeToMenuEventsInternal(TMenu menu) { }
        protected virtual void UnsubscribeFromMenuEventsInternal(TMenu menu) { }

        // React to becoming / stopping being the top-most menu (the view is already notified).
        protected virtual void OnFocusGainedInternal() { }
        protected virtual void OnFocusLostInternal() { }

        protected virtual UniTask PreShowInternal(TMenu menu) => UniTask.CompletedTask;
        protected virtual UniTask PostShowInternal(TMenu menu) => UniTask.CompletedTask;
        protected virtual UniTask PreHideInternal(TMenu menu) => UniTask.CompletedTask;
        protected virtual UniTask PostHideInternal(TMenu menu) => UniTask.CompletedTask;
    }

    public abstract class AbstractMenuPresenter<TMenu, TData> : AbstractMenuPresenter<TMenu>, IMenuPresenter<TData>
        where TMenu : Menu
    {
        [Inject] protected readonly UIManager UIManager;

        protected TData Data { get; private set; }

        public async UniTask Show(TData data)
        {
            SetData(data);
            await Show();
        }

        private void SetData(TData data)
        {
            if (Data != null) ClearData();

            Data = data;
            SetDataInternal(data);
            SubscribeToDataEventsInternal(data);
        }

        private void ClearData()
        {
            if (Data == null) return;

            UnsubscribeToDataEventsInternal(Data);
            ClearDataInternal();
            Data = default;
        }

        protected override async UniTask PostHideInternal(TMenu menu)
        {
            await base.PostHideInternal(menu);
            ClearData();
        }

        protected virtual void SetDataInternal(TData data) { }
        protected virtual void ClearDataInternal() { }
        protected virtual void SubscribeToDataEventsInternal(TData data) { }
        protected virtual void UnsubscribeToDataEventsInternal(TData data) { }
    }
}
