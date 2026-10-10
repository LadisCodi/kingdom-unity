using Codigames.Game.Audio;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Modules.Audio;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // A confirmation's logic: the close and Cancel leave without an answer; the action closes the window first, then
    // runs. A popup: the screen that asked stays up under it, with its state.
    public class ConfirmMenuPresenter : AbstractDataMenuPresenter<ConfirmMenu, ConfirmData>, IPopupMenuPresenter
    {
        private readonly UIManager _ui;
        private readonly ISoundService _sounds;

        public ConfirmMenuPresenter(IMenuViewFactory views, UIManager ui, ISoundService sounds) : base(views)
        {
            _ui = ui;
            _sounds = sounds;
        }

        public void RequestClose() => _ = _ui.HideMenu<ConfirmMenu>();

        protected override void BindInternal(ConfirmMenu view)
            => view.Show(Data.Title, Data.Art, Data.Text, Data.Note, Data.CancelLabel, Data.OkLabel);

        protected override void SubscribeToViewEventsInternal(ConfirmMenu view)
        {
            view.CloseTapped += RequestClose;
            view.OkTapped += OnOk;
        }

        protected override void UnsubscribeFromViewEventsInternal(ConfirmMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.OkTapped -= OnOk;
        }

        private async void OnOk()
        {
            var ok = Data.Ok;
            _sounds.Play(SoundIds.BUTTON_PRESS);
            await _ui.HideMenu<ConfirmMenu>();
            ok?.Invoke();
        }
    }
}
