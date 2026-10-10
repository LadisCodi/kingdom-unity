using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Unlocks;
using Codigames.Kingdom.Doors;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.Store
{
    // When the payer is asked (the web's Game.payerDue): once the First Morning is over and no profile is chosen, on
    // the bare map — not over a sheet, nor under an unlock's splash. While it is due the stage holds back.
    public class PayerGate : ITickable
    {
        private readonly Kingdom.Store.Store _store;
        private readonly Doors _doors;
        private readonly UIManager _ui;
        private readonly UnlockSplashPresenter _unlocks;

        public PayerGate(Kingdom.Store.Store store, Doors doors, UIManager ui, UnlockSplashPresenter unlocks)
        {
            _store = store;
            _doors = doors;
            _ui = ui;
            _unlocks = unlocks;
        }

        public bool Due => _store.Profile == null && !_doors.FirstMorningOn;

        public void Tick()
        {
            if (!Due || _ui.HasOverlayOpen || _unlocks.IsPending || _ui.IsShown<PayerMenu>()) return;
            _ = _ui.ShowMenu<PayerMenu>();
        }
    }
}
