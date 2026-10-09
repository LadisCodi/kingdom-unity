using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Notices;
using Codigames.Kingdom.Notices;
using Codigames.Modules.Audio;
using Codigames.Modules.UI;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // Keeps the news column to the inbox (Docs/features/26-notices.md §3–§4): redrawn when what it shows changes, a
    // new bubble popping in with a chime, hidden while a menu covers the map. A news left unread goes on its own after
    // ten seconds on screen, blinking for its last three — only the time it is seen counts, and a news that gains an
    // item starts again. A tap opens the bubble's card. Persistent.
    public class NoticesColumnPresenter : AbstractMenuPresenter<NoticesColumn>, ITickable
    {
        private const float LIFE_SECONDS = 10;
        private const float WARN_SECONDS = 3;
        // One beat at most: a pause must not age every bubble by the whole time it lasted.
        private const float MAX_BEAT = 2;

        // News whose event already sounds where it happens: its bubble arriving adds no chime.
        private static readonly HashSet<string> VOICED = new() { "news:" + NewsGroup.Built };

        private readonly NoticeBoard _board;
        private readonly Inbox _inbox;
        private readonly UIManager _ui;
        private readonly ISoundService _sounds;
        private readonly Dictionary<string, (int Count, float Seen)> _life = new();

        private IReadOnlyList<Notice> _shown = new List<Notice>();
        private string _drawn;
        private HashSet<string> _seen;

        public NoticesColumnPresenter(IMenuViewFactory views, NoticeBoard board, Inbox inbox, UIManager ui, ISoundService sounds) : base(views)
        {
            _board = board;
            _inbox = inbox;
            _ui = ui;
            _sounds = sounds;
        }

        protected override void BindInternal(NoticesColumn view)
        {
            _inbox.Changed += Refresh;
            _ui.MenuWillShow += OnMenus;
            _ui.MenuShown += OnMenus;
            _ui.MenuHidden += OnMenus;
            Refresh();
        }

        protected override void UnbindInternal(NoticesColumn view)
        {
            _inbox.Changed -= Refresh;
            _ui.MenuWillShow -= OnMenus;
            _ui.MenuShown -= OnMenus;
            _ui.MenuHidden -= OnMenus;
        }

        protected override void SubscribeToViewEventsInternal(NoticesColumn view) => view.BubbleTapped += OnBubbleTapped;

        protected override void UnsubscribeFromViewEventsInternal(NoticesColumn view) => view.BubbleTapped -= OnBubbleTapped;

        public void Tick()
        {
            if (!IsShown) return;
            var beat = Mathf.Min(Time.unscaledDeltaTime, MAX_BEAT);
            var visible = !_ui.HasOverlayOpen;
            string expired = null;
            foreach (var notice in _shown.Where(n => n.Kind == NoticeKind.News))
            {
                // Counted from now: the beat that brought it in is not its time.
                var life = _life.TryGetValue(notice.Id, out var was) && was.Count == notice.Count ? (notice.Count, was.Seen + (visible ? beat : 0)) : (notice.Count, 0f);
                _life[notice.Id] = life;
                View.SetLeaving(notice.Id, life.Item2 >= LIFE_SECONDS - WARN_SECONDS);
                if (life.Item2 >= LIFE_SECONDS) expired = notice.Id;
            }

            if (expired == null) return;
            _life.Remove(expired);
            _board.Read(expired);
        }

        private void OnMenus(IMenuPresenter menu) => Refresh();

        private void Refresh()
        {
            if (!IsShown) return;
            var hidden = _ui.HasOverlayOpen;
            View.SetHidden(hidden);
            if (hidden) return;

            _shown = _board.Column;
            var signature = string.Join("|", _shown.Select(n => $"{n.Id}:{n.Art?.name}:{n.Count}:{n.Carved}"));
            if (signature == _drawn) return;
            _drawn = signature;

            var ids = new HashSet<string>(_shown.Select(n => n.Id));
            var arrived = _seen == null ? new List<string>() : _shown.Where(n => n.Kind != NoticeKind.More && !_seen.Contains(n.Id)).Select(n => n.Id).ToList();
            if (arrived.Any(id => !VOICED.Contains(id))) _sounds.Play(SoundIds.POP);
            _seen = ids;
            foreach (var gone in _life.Keys.Where(id => !ids.Contains(id)).ToList()) _life.Remove(gone);
            View.Show(_shown, arrived);
        }

        private void OnBubbleTapped(string id)
        {
            _sounds.Play(SoundIds.BUTTON_PRESS);
            _ = _ui.ShowMenu<NoticeCardMenu, string>(id);
        }
    }
}
