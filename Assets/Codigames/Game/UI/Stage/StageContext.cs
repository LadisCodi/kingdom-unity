using Codigames.Game.UI.Unlocks;
using Codigames.Kingdom.Tutorial;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Stage
{
    // What the screen says about whether a scene may start. An unlock splash due or on screen holds every scene back
    // (fights, reveals and videos will too); the world board does not exist yet.
    public class StageContext : IStageContext
    {
        private readonly UIManager _ui;
        private readonly UnlockSplashPresenter _unlocks;
        private readonly Codigames.Game.Store.PayerGate _payer;

        public StageContext(UIManager ui, UnlockSplashPresenter unlocks, Codigames.Game.Store.PayerGate payer = null)
        {
            _ui = ui;
            _unlocks = unlocks;
            _payer = payer;
        }

        // An unlock splash due, or the payer still to be asked.
        public bool HeldBack => _unlocks.IsPending || (_payer != null && _payer.Due);

        public bool OnWorld => false;

        public bool HasOpenSheet => _ui.HasOverlayOpen;
    }
}
