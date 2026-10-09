using System;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Research;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // Keeps the Knowledge tab under the plank to the bar: what is held, the phial dripping in, when the next
    // point and the whole bar arrive. It steps up under the plank for every menu but the ones that spend
    // Knowledge, and a tap opens the Knowledge sheet.
    public class KnowledgeTabPresenter : IStartable, ITickable, IDisposable
    {
        private const double REFRESH_MS = 200;

        private readonly IMenuViewFactory _views;
        private readonly UIManager _ui;
        private readonly KnowledgeBar _bar;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;

        private readonly Kingdom.Doors.Doors _doors;

        private HeaderMenu _header;
        private double _shownAt = double.MinValue;

        public KnowledgeTabPresenter(IMenuViewFactory views, UIManager ui, KnowledgeBar bar, IClock clock, NumberFormat numbers,
            Localizer localizer, Kingdom.Doors.Doors doors)
        {
            _doors = doors;
            _views = views;
            _ui = ui;
            _bar = bar;
            _clock = clock;
            _numbers = numbers;
            _localizer = localizer;
        }

        public void Start()
        {
            _header = _views.Resolve<HeaderMenu>();
            if (_header == null || _header.Knowledge == null) return;

            _header.Knowledge.Tapped += OnTapped;
            _ui.MenuWillShow += OnMenuWillShow;
            _ui.MenuHidden += OnMenuHidden;
        }

        public void Dispose()
        {
            if (_header == null || _header.Knowledge == null) return;

            _header.Knowledge.Tapped -= OnTapped;
            _ui.MenuWillShow -= OnMenuWillShow;
            _ui.MenuHidden -= OnMenuHidden;
        }

        public void Tick()
        {
            if (_header == null || _header.Knowledge == null) return;

            // A readout with nothing to read yet is absent: the bar comes with the books.
            _header.Knowledge.gameObject.SetActive(_doors.IsOpen(Kingdom.Doors.DoorId.Knowledge));

            var now = _clock.NowMs;
            if (now - _shownAt < REFRESH_MS) return;

            _shownAt = now;
            var held = (int)Math.Floor(_bar.Amount);
            var cap = (int)_bar.Cap;
            var next = _bar.NextUnitAt;
            var unitMs = 3_600_000 / _bar.PerHour;
            var fraction = next == null ? 0f : (float)Math.Clamp(1 - (next.Value - now) / unitMs, 0, 1);
            var nextIn = next == null ? "" : _localizer.Tr("+1 in {time}", ("time", _numbers.Countdown(Math.Ceiling((next.Value - now) / 1000))));
            var fullMs = _bar.FullAt(now) - now;
            var fullIn = next == null || fullMs <= 0 ? "" : _localizer.Tr("Full in {time}", ("time", _numbers.Countdown(Math.Ceiling(fullMs / 1000))));
            _header.Knowledge.Show(held, cap, fraction, held >= cap ? _localizer.Tr("Full") : nextIn, fullIn);
        }

        private void OnTapped() => _ = _ui.ShowMenu<KnowledgeSheetMenu>();

        // It stays down over the menus that spend Knowledge, and steps aside for every other one.
        private void OnMenuWillShow(IMenuPresenter menu)
        {
            if (menu is IClosableMenuPresenter && !KeepsTab(menu)) _header.Knowledge.SetTucked(true);
        }

        private void OnMenuHidden(IMenuPresenter menu)
        {
            var other = _ui.HasOverlayOpen && !_ui.IsShown<ResearchMenu>() && !_ui.IsShown<TechSheetMenu>() && !_ui.IsShown<KnowledgeSheetMenu>();
            _header.Knowledge.SetTucked(other);
        }

        private static bool KeepsTab(IMenuPresenter menu)
            => menu is ResearchMenuPresenter or TechSheetMenuPresenter or KnowledgeSheetMenuPresenter;
    }
}
