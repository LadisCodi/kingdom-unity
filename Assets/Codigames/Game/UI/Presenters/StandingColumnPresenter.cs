using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Notices;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // Keeps the standing column to what is true now: redrawn when what it shows changes, each countdown rewritten as it
    // runs, hidden while a menu covers the map. A tap opens the bubble's card. Persistent.
    public class StandingColumnPresenter : AbstractMenuPresenter<StandingColumn>, ITickable
    {
        private readonly NoticeBoard _board;
        private readonly UIManager _ui;
        private readonly ISoundService _sounds;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Dictionary<string, string> _clocks = new();

        private IReadOnlyList<Notice> _shown = new List<Notice>();
        private string _drawn;

        public StandingColumnPresenter(IMenuViewFactory views, NoticeBoard board, UIManager ui, ISoundService sounds, IClock clock,
            NumberFormat numbers) : base(views)
        {
            _board = board;
            _ui = ui;
            _sounds = sounds;
            _clock = clock;
            _numbers = numbers;
        }

        protected override void BindInternal(StandingColumn view)
        {
            _ui.MenuWillShow += OnMenus;
            _ui.MenuShown += OnMenus;
            _ui.MenuHidden += OnMenus;
            Refresh();
        }

        protected override void UnbindInternal(StandingColumn view)
        {
            _ui.MenuWillShow -= OnMenus;
            _ui.MenuShown -= OnMenus;
            _ui.MenuHidden -= OnMenus;
        }

        protected override void SubscribeToViewEventsInternal(StandingColumn view) => view.BubbleTapped += OnBubbleTapped;

        protected override void UnsubscribeFromViewEventsInternal(StandingColumn view) => view.BubbleTapped -= OnBubbleTapped;

        // What is true changes with the clock (a raid lands, the next takes its place): read every frame, drawn on change.
        public void Tick()
        {
            if (!IsShown) return;
            Refresh();
            var now = _clock.NowMs;
            foreach (var notice in _shown.Where(n => n.Until.HasValue))
            {
                var text = _numbers.Countdown(System.Math.Ceiling(System.Math.Max(0, (notice.Until.Value - now) / 1000)));
                if (_clocks.TryGetValue(notice.Id, out var was) && was == text) continue;
                _clocks[notice.Id] = text;
                View.SetClock(notice.Id, text);
            }
        }

        private void OnMenus(IMenuPresenter menu) => Refresh();

        private void Refresh()
        {
            if (!IsShown) return;
            var hidden = _ui.HasOverlayOpen;
            View.SetHidden(hidden);
            if (hidden) return;

            _shown = _board.Standing;
            var signature = string.Join("|", _shown.Select(n => $"{n.Id}:{n.Art?.name}:{n.Count}:{n.Until}"));
            if (signature == _drawn) return;
            var arrived = _drawn == null ? new List<string>() : _shown.Select(n => n.Id).Where(id => !_drawn.Contains(id + ":")).ToList();
            _drawn = signature;
            _clocks.Clear();
            View.Show(_shown, arrived);
        }

        private void OnBubbleTapped(string id)
        {
            _sounds.Play(SoundIds.BUTTON_PRESS);
            _ = _ui.ShowMenu<NoticeCardMenu, string>(id);
        }
    }
}
