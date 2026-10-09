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

        public StageContext(UIManager ui, UnlockSplashPresenter unlocks)
        {
            _ui = ui;
            _unlocks = unlocks;
        }

        public bool HeldBack => _unlocks.IsPending;

        public bool OnWorld => false;

        public bool HasOpenSheet => _ui.HasOverlayOpen;
    }
}
