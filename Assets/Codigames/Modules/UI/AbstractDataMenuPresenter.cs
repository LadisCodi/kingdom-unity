using System.Threading.Tasks;

namespace Codigames.Modules.UI
{
    // A presenter whose menu is shown with data: the data is set before the show and cleared after the hide.
    public abstract class AbstractDataMenuPresenter<TView, TData> : AbstractMenuPresenter<TView>, IDataMenuPresenter<TData>
        where TView : class, IMenuView
    {
        protected AbstractDataMenuPresenter(IMenuViewFactory views) : base(views)
        {
        }

        protected TData Data { get; private set; }

        private bool HasData { get; set; }

        public async Task Show(TData data)
        {
            SetData(data);
            await Show();
        }

        protected override async Task PostHideInternal(TView view)
        {
            await base.PostHideInternal(view);
            ClearData();
        }

        private void SetData(TData data)
        {
            ClearData();

            Data = data;
            HasData = true;
            SetDataInternal(data);
            SubscribeToDataEventsInternal(data);
        }

        private void ClearData()
        {
            if (!HasData) return;

            UnsubscribeFromDataEventsInternal(Data);
            ClearDataInternal();
            Data = default;
            HasData = false;
        }

        protected virtual void SetDataInternal(TData data) { }
        protected virtual void ClearDataInternal() { }
        protected virtual void SubscribeToDataEventsInternal(TData data) { }
        protected virtual void UnsubscribeFromDataEventsInternal(TData data) { }
    }
}
