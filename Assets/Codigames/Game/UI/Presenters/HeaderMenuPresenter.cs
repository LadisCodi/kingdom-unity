using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data.Economy;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Magic;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // Keeps the header to the treasury: which currencies are on the plank (a contextual coin appears once the
    // kingdom holds some) and how much of each. Mana is a pool: its slot is a gauge, and while it fills its
    // amount takes turns with the next unit's countdown. Persistent: shown when the game starts, never closed.
    public class HeaderMenuPresenter : AbstractMenuPresenter<HeaderMenu>, ITickable
    {
        // The pool shows this long, then the countdown this long, and round again.
        private const double POOL_MS = 3000;
        private const double NEXT_MS = 3000;

        private readonly ITreasury _treasury;
        private readonly IPlankCurrencies _currencies;
        private readonly ManaPool _mana;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;

        private string _shown;
        private string _manaText;

        public HeaderMenuPresenter(IMenuViewFactory views, ITreasury treasury, IPlankCurrencies currencies, ManaPool mana,
            IClock clock, NumberFormat numbers, Localizer localizer) : base(views)
        {
            _treasury = treasury;
            _currencies = currencies;
            _mana = mana;
            _clock = clock;
            _numbers = numbers;
            _localizer = localizer;
        }

        public void Tick()
        {
            if (IsShown) ShowMana();
        }

        protected override void BindInternal(HeaderMenu view)
        {
            _shown = null;
            Refresh();
            _treasury.Changed += OnChanged;
        }

        protected override void UnbindInternal(HeaderMenu view) => _treasury.Changed -= OnChanged;

        private void OnChanged(string currency, double amount) => Refresh();

        private void Refresh()
        {
            var visible = Visible().ToList();
            var key = string.Join(",", visible.Select(c => c.Id));

            if (key != _shown)
            {
                _shown = key;
                View.SetSlots(visible.Select(c => (c.Id, c.Icon, c.Sold, c.Place == PlankPlace.Right)));
            }

            // Rolled up past ten thousand, so a balance never outgrows its slot.
            foreach (var slot in View.Slots)
            {
                if (slot.CurrencyId == ManaPool.MANA) continue;

                slot.SetAmount(_numbers.Count(_treasury.Get(slot.CurrencyId)));
                slot.SetGauge(null);
            }

            _manaText = null;
            ShowMana();
        }

        private void ShowMana()
        {
            var slot = View.Slots.FirstOrDefault(s => s.CurrencyId == ManaPool.MANA);
            if (slot == null) return;

            var now = _clock.NowMs;
            var next = _mana.NextUnitAt;
            var turn = next.HasValue && now % (POOL_MS + NEXT_MS) >= POOL_MS;
            var text = turn
                ? _localizer.Tr("+1 in {time}", ("time", _numbers.Countdown(Math.Max(0, next.Value - now) / 1000)))
                : _numbers.Count(_mana.Amount);

            if (text == _manaText) return;
            _manaText = text;
            slot.SetAmount(text, turn);
            slot.SetGauge((float)(_mana.Amount / _mana.Cap));
        }

        private IEnumerable<IPlankCurrency> Visible()
            => _currencies.OnPlank.Where(c => c.Place != PlankPlace.CoinWhenHeld || _treasury.Get(c.Id) > 0);
    }
}
