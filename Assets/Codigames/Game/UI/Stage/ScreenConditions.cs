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
    // is on screen, how many taps since a line began, what is placed or moved. The world board is not in the game
    // yet: never true.
    public class ScreenConditions : IConditionReader
    {
        // The web's sheet names, as the scenes say them.
        private static readonly Dictionary<string, Type> SHEETS = new()
        {
            ["build"] = typeof(BuildMenu),
            ["research"] = typeof(ResearchMenu),
            ["knowledge"] = typeof(KnowledgeSheetMenu),
            ["bag"] = typeof(BagMenu),
            ["lair"] = typeof(AttackSheetMenu),
            ["relicPicker"] = typeof(RelicPickerMenu),
        };

        private readonly UIManager _ui;
        private readonly PlacementMenuPresenter _placement;
        private readonly RuinCardMenuPresenter _ruinCard;
        private readonly SitesState _sites;
        private readonly UiTargets _targets;
        private readonly TapCount _taps;

        private readonly RelicPickerMenuPresenter _relicPicker;
        private readonly Kingdom.Tutorial.ReachSpots _reach;
        private readonly Kingdom.City.Placement _rules;

        public ScreenConditions(UIManager ui, PlacementMenuPresenter placement, RuinCardMenuPresenter ruinCard, SitesState sites,
            UiTargets targets, TapCount taps, RelicPickerMenuPresenter relicPicker, Kingdom.Tutorial.ReachSpots reach, Kingdom.City.Placement rules)
        {
            _relicPicker = relicPicker;
            _reach = reach;
            _rules = rules;
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
                ConditionKind.Placing => _placement.PlacingId == c.Target,
                ConditionKind.Moving => _placement.MovingId == c.Target,
                ConditionKind.Taps => _taps.Taps - tapsAtStart >= c.AtLeast(),
                // A ruin's card open, or the ruin already repaired.
                ConditionKind.SiteOpen => _sites.Repaired.Contains(c.Target) || _ruinCard.Shows(c.Target),
                ConditionKind.GhostReaches => GhostReaches(c),
                ConditionKind.ReachCleared => (_reach.Best(c.Target, true)?.Works ?? 0) >= c.AtLeast(),
                ConditionKind.RelicPicked => _relicPicker.IsShown && _relicPicker.Slot != null && (c.Target == string.Empty || _relicPicker.Slot == c.Target),
                ConditionKind.WorldOpen => false,
                _ => null,
            };

            holds = answer ?? false;
            return answer.HasValue;
        }

        // The ghost of the building stands where it may, and its crew would work that many cells there.
        private bool GhostReaches(Condition c)
        {
            var id = _placement.PlacingId ?? (_placement.MovingDistrictId != null ? _placement.MovingId : null);
            if (id != c.Target || _placement.GhostAnchor is not { } anchor) return false;
            if (_rules.Check(id, anchor, _placement.MovingDistrictId) != Kingdom.City.PlacementProblem.None) return false;
            return _reach.WorksAt(id, anchor, _placement.MovingDistrictId) >= c.AtLeast();
        }
    }
}
