using System.Linq;
using Codigames.Game.Data.Tutorial;
using Codigames.Game.Map;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Map;
using Codigames.Kingdom.Quests;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Tutorial;
using Codigames.Modules.Cameras;
using Codigames.Modules.Core;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Quests
{
    // "Show me": a tap on a running quest points at its goal — the camera glides to the cell to go and clear or the
    // ruin to repair, and the stage's hand points at it; or the menu where it is done opens.
    public class QuestFocus
    {
        private readonly UIManager _ui;
        private readonly CameraController _camera;
        private readonly ProvinceMap _map;
        private readonly IProvinceMap _province;
        private readonly FogOfWar _fog;
        private readonly CityState _city;
        private readonly GroundState _ground;
        private readonly Ruins _ruins;
        private readonly Landmarks _landmarks;
        private readonly QuestGoals _goals;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly StageHint _hint;
        private readonly IStageSettings _stage;

        public QuestFocus(UIManager ui, CameraController camera, ProvinceMap map, IProvinceMap province, FogOfWar fog, CityState city,
            GroundState ground, Ruins ruins, Landmarks landmarks, QuestGoals goals, ICatalog<IBuildingDefinition> buildings, StageHint hint,
            IStageSettings stage)
        {
            _hint = hint;
            _stage = stage;
            _ui = ui;
            _camera = camera;
            _map = map;
            _province = province;
            _fog = fog;
            _city = city;
            _ground = ground;
            _ruins = ruins;
            _landmarks = landmarks;
            _goals = goals;
            _buildings = buildings;
        }

        public void Show(IQuestDefinition quest)
        {
            var target = quest.GoalTarget;
            switch (quest.GoalType)
            {
                case GoalType.RepairDistrict:
                case GoalType.BuildDistrict:
                {
                    // A ruin of its kind still standing is the way to build it.
                    var ruin = _ruins.Standing.FirstOrDefault(r => _goals.Names(target, r.District));
                    if (ruin != null) Centre(ruin.Anchor, _buildings.Get(ruin.District).Width, _buildings.Get(ruin.District).Height);
                    else _ = _ui.ShowMenu<BuildMenu>();
                    break;
                }
                case GoalType.UpgradeDistrict:
                {
                    var district = Built(d => _goals.Names(target, d.DefinitionId) && d.Level < quest.GoalLevel) ?? Built(d => _goals.Names(target, d.DefinitionId));
                    if (district != null) Inspect(district);
                    else _ = _ui.ShowMenu<BuildMenu>();
                    break;
                }
                case GoalType.ReachPopulation:
                    Inspect(Built(d => d.DefinitionId == "Townhall"));
                    break;
                case GoalType.AssignWorkers:
                    Inspect(Built(d => _buildings.Get(d.DefinitionId).Production.MaxWorkersPerLevel.Count > 0));
                    break;
                case GoalType.WorkInReach:
                    Inspect(_city.Districts.FirstOrDefault(d => d.DefinitionId == target));
                    break;
                case GoalType.CompleteTech:
                case GoalType.CompleteTechs:
                    _ = _ui.ShowMenu<ResearchMenu>();
                    break;
                case GoalType.DiscoverFeature:
                    Centre(Nearest(c => _fog.VisibilityAt(c) == Visibility.Discovered && _ground.Features.TryGetValue(c, out var f) && f == target)
                           ?? Nearest(_fog.IsPayable));
                    break;
                case GoalType.ClaimLandmarks:
                {
                    var landmark = _landmarks.All.Where(l => !_landmarks.IsClaimed(l.Id) && (target == null || l.Kind == target)
                                                             && _fog.VisibilityAt(l.Anchor) != Visibility.Undiscovered)
                        .OrderBy(l => _fog.Rings(l.Anchor)).FirstOrDefault();
                    if (landmark != null) Centre(landmark.Anchor, landmark.Size, landmark.Size);
                    else Centre(Nearest(_fog.IsPayable));
                    break;
                }
                default:
                    Centre(Nearest(_fog.IsPayable));
                    break;
            }
        }

        private DistrictState Built(System.Func<DistrictState, bool> match) => _city.Districts.FirstOrDefault(d => d.Built && match(d));

        private void Inspect(DistrictState district)
        {
            if (district == null) return;
            _ = _ui.ShowMenu<DistrictCardMenu, string>(district.Id);
        }

        // The cell nearest the Townhall that matches.
        private Vector2Int? Nearest(System.Func<Vector2Int, bool> match)
            => _province.Cells.Where(match).OrderBy(_fog.Rings).ThenBy(c => c.Y).ThenBy(c => c.X).Select(c => (Vector2Int?)c).FirstOrDefault();

        private void Centre(Vector2Int? cell, int width = 1, int height = 1)
        {
            if (cell == null) return;
            var corners = ProvinceGeometry.Corners(_map, cell.Value, width, height);
            var world = (corners[0] + corners[2]) / 2f;
            _camera.CenterOn(new Codigames.Modules.Core.Vector2(world.x, world.y));
            _hint.Point(new MapTarget(cell.Value, width, height), _stage.PointerSeconds);
        }
    }
}
