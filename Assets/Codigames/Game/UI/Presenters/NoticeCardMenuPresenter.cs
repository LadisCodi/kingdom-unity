using Codigames.Game.Audio;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Notices;
using Codigames.Modules.Audio;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // A notice's card: opening it reads its news, so the bubble goes at once while the card keeps what it said. Go
    // closes the card and takes the player to the subject; on the +N's card a row opens that notice's own card.
    public class NoticeCardMenuPresenter : AbstractDataMenuPresenter<NoticeCardMenu, string>, IClosableMenuPresenter
    {
        private readonly NoticeBoard _board;
        private readonly UIManager _ui;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;

        private Notice _notice;

        public NoticeCardMenuPresenter(IMenuViewFactory views, NoticeBoard board, UIManager ui, Localizer localizer, ISoundService sounds)
            : base(views)
        {
            _board = board;
            _ui = ui;
            _localizer = localizer;
            _sounds = sounds;
        }

        public void RequestClose() => _ = _ui.HideMenu<NoticeCardMenu>();

        protected override void BindInternal(NoticeCardMenu view) => Present(Data);

        protected override void SubscribeToViewEventsInternal(NoticeCardMenu view)
        {
            view.CloseTapped += RequestClose;
            view.GoTapped += OnGo;
            view.RowTapped += OnRow;
        }

        protected override void UnsubscribeFromViewEventsInternal(NoticeCardMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.GoTapped -= OnGo;
            view.RowTapped -= OnRow;
        }

        private void Present(string id)
        {
            _notice = _board.Of(id) ?? new Notice { Title = _localizer.Tr("Notices"), Body = _localizer.Tr("Nothing new.") };
            _board.Read(id);
            View.Show(_notice, Go, _localizer.Tr("Go"), _localizer.Tr("Open"));
        }

        private string Go => "<sprite name=\"compass\"> " + _localizer.Tr("Go");

        private void OnGo()
        {
            _sounds.Play(SoundIds.BUTTON_PRESS);
            _notice?.Go?.Invoke();
        }

        private void OnRow(NoticeRowData row)
        {
            _sounds.Play(SoundIds.BUTTON_PRESS);
            if (row.Opens != null) Present(row.Opens);
            else row.Go?.Invoke();
        }
    }
}
