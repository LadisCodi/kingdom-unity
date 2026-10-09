using Codigames.Kingdom.Tutorial;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Stage
{
    // What the screen says about whether a scene may start. Nothing holds scenes back yet (no fights, reveals,
    // videos or unlock splashes in the game), and the world board does not exist yet.
    public class StageContext : IStageContext
    {
        private readonly UIManager _ui;

        public StageContext(UIManager ui) => _ui = ui;

        public bool HeldBack => false;

        public bool OnWorld => false;

        public bool HasOpenSheet => _ui.HasOverlayOpen;
    }
}
