using System;
using System.Collections.Generic;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Presenters;
using Codigames.Kingdom.Sites.State;
using Codigames.Kingdom.Tutorial;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Stage
{
    // The conditions the screen answers: which sheet is open, what is being placed, which card is up, what control
    // is on screen, how many taps since a line began. The world board and moving a building are not in the game
    // yet: never true.
    public class ScreenConditions : IConditionReader
    {
        // The web's sheet names, as the scenes say them.
        private static readonly Dictionary<string, Type> SHEETS = new()
        {
            ["build"] = typeof(BuildMenu),
            ["research"] = typeof(ResearchMenu),
            ["knowledge"] = typeof(KnowledgeSheetMenu),
        };

        private readonly UIManager _ui;
        private readonly PlacementMenuPresenter _placement;
        private readonly RuinCardMenuPresenter _ruinCard;
        private readonly SitesState _sites;
        private readonly UiTargets _targets;
        private readonly TapCount _taps;

        public ScreenConditions(UIManager ui, PlacementMenuPresenter placement, RuinCardMenuPresenter ruinCard, SitesState sites,
            UiTargets targets, TapCount taps)
        {
            _ui = ui;
            _placement = placement;
            _ruinCard = ruinCard;
            _sites = sites;
            _targets = targets;
            _taps = taps;
        }

        public bool TryHolds(Condition c, int tapsAtStart, out bool holds)
        {
            bool? answer = c.Kind switch
            {
                ConditionKind.Overlay => SHEETS.TryGetValue(c.Target, out var sheet) && _ui.IsShown(sheet),
                ConditionKind.NoOverlay => !_ui.HasOverlayOpen,
                // Back on the map: no sheet, no card, no placing.
                ConditionKind.MainScreen => !_ui.HasOverlayOpen,
                ConditionKind.Ui => _targets.Find(c.Target) != null,
                ConditionKind.Placing => _placement.Shows(c.Target),
                ConditionKind.Taps => _taps.Taps - tapsAtStart >= c.AtLeast(),
                // A ruin's card open, or the ruin already repaired.
                ConditionKind.SiteOpen => _sites.Repaired.Contains(c.Target) || _ruinCard.Shows(c.Target),
                ConditionKind.Moving or ConditionKind.GhostReaches or ConditionKind.WorldOpen => false,
                _ => null,
            };

            holds = answer ?? false;
            return answer.HasValue;
        }
    }
}
