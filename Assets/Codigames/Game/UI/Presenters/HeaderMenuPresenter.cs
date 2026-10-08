using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data.Economy;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // Keeps the header to the treasury: which currencies are on the plank (a contextual coin appears once the
    // kingdom holds some) and how much of each. Persistent: shown when the game starts, never closed.
    public class HeaderMenuPresenter : AbstractMenuPresenter<HeaderMenu>
    {
        private readonly ITreasury _treasury;
        private readonly IPlankCurrencies _currencies;
        private readonly NumberFormat _numbers;

        private string _shown;

        public HeaderMenuPresenter(IMenuViewFactory views, ITreasury treasury, IPlankCurrencies currencies, NumberFormat numbers)
            : base(views)
        {
            _treasury = treasury;
            _currencies = currencies;
            _numbers = numbers;
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
                slot.SetAmount(_numbers.Count(_treasury.Get(slot.CurrencyId)));
            }
        }

        private IEnumerable<IPlankCurrency> Visible()
            => _currencies.OnPlank.Where(c => c.Place != PlankPlace.CoinWhenHeld || _treasury.Get(c.Id) > 0);
    }
}
