using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data.City;
using Codigames.Game.Map;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Crews.State;
using Codigames.Modules.Clock;
using UnityEngine;
using VContainer;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Crews
{
    // The crews at work on the map, as the web draws them: a worker waits by its building's door, walks to its
    // cell and back, and swings while it works, its feet on the ground and facing the way it walks. Each worker is
    // drawn from its building's cast, by a stable hash of its id, so it keeps its face.
    public class CrewsView : MonoBehaviour
    {
        // How far from its building's middle a waiting worker stands, past the footprint's edge, in cells.
        private const float DOOR_DISTANCE = 0.55f;

        [SerializeField] private CharacterCatalog _characters;
        [SerializeField] private SpriteRenderer _memberPrefab;

        private readonly Dictionary<string, SpriteRenderer> _members = new();
        private readonly List<string> _gone = new();

        private CityState _city;
        private Codigames.Kingdom.Crews.Workforce _crews;
        private BuildingCollection _buildings;
        private ProvinceMap _map;
        private IClock _clock;
        private Vector3 _origin;
        private Vector3 _alongX;
        private Vector3 _alongY;

        [Inject]
        public void Construct(CityState city, Codigames.Kingdom.Crews.Workforce crews, BuildingCollection buildings, ProvinceMap map, IClock clock)
        {
            _city = city;
            _crews = crews;
            _buildings = buildings;
            _map = map;
            _clock = clock;
        }

        private void Start()
        {
            _origin = _map.CellCentre(new ModuleVector2Int(0, 0));
            _alongX = _map.CellCentre(new ModuleVector2Int(1, 0)) - _origin;
            _alongY = _map.CellCentre(new ModuleVector2Int(0, 1)) - _origin;
        }

        private void LateUpdate()
        {
            if (_city == null) return;

            var now = _clock.NowMs;
            var seconds = Time.time;

            foreach (var worker in _city.Workers)
            {
                var building = _city.Districts.FirstOrDefault(d => d.Id == worker.BuildingId);
                if (building == null) continue;

                var member = MemberOf(worker);
                var asset = _buildings.Get<BuildingAsset>(building.DefinitionId);
                var phase = Phase(worker.Id);
                var cast = asset.Crew.Count == 0 ? null : asset.Crew[(int)(phase * 1000) % asset.Crew.Count];

                var (position, pose, facesRight) = Place(worker, building, asset, now, phase);
                member.transform.position = position;
                member.flipX = facesRight;
                member.sprite = cast == null ? null : _characters.Frame(cast, pose, seconds + phase * 10);
            }

            // Workers sent home leave the map.
            _gone.Clear();
            foreach (var id in _members.Keys)
            {
                if (!_city.Workers.Any(w => w.Id == id)) _gone.Add(id);
            }

            foreach (var id in _gone)
            {
                Destroy(_members[id].gameObject);
                _members.Remove(id);
            }
        }

        private (Vector3 Position, Pose Pose, bool FacesRight) Place(WorkerState worker, DistrictState building, BuildingAsset asset,
            double now, float phase)
        {
            var home = new Vector2(building.Anchor.X, building.Anchor.Y);

            if (worker.Activity == WorkerActivity.Idle)
            {
                var angle = phase * Mathf.PI * 2f;
                var spot = home + new Vector2((asset.Width - 1) / 2f + Mathf.Cos(angle) * (asset.Width / 2f + DOOR_DISTANCE),
                    (asset.Height - 1) / 2f + Mathf.Sin(angle) * (asset.Height / 2f + DOOR_DISTANCE));
                return (World(spot), Pose.Idle, false);
            }

            var cell = worker.ClaimedCell.HasValue ? new Vector2(worker.ClaimedCell.Value.X, worker.ClaimedCell.Value.Y) : home;
            if (worker.Activity == WorkerActivity.Working) return (World(cell), Pose.Work, false);

            var t = (float)_crews.Progress(worker, now);
            var at = Vector2.Lerp(home, cell, t);

            // Facing across the screen, not the grid: a step in +y goes left on screen.
            var outward = worker.Activity == WorkerActivity.MovingToCell ? 1f : -1f;
            var screenDx = ((cell.x - home.x) - (cell.y - home.y)) * outward;
            return (World(at), Pose.Walk, screenDx > 0);
        }

        // A point in fractional cells, as a world position on the ground (a cell's centre at whole numbers).
        private Vector3 World(Vector2 cell) => _origin + _alongX * cell.x + _alongY * cell.y;

        private SpriteRenderer MemberOf(WorkerState worker)
        {
            if (_members.TryGetValue(worker.Id, out var member)) return member;

            member = Instantiate(_memberPrefab, transform);
            member.name = worker.Id;
            _members[worker.Id] = member;
            return member;
        }

        // A stable fraction per worker, so it keeps its face and its spot by the door.
        private static float Phase(string id)
        {
            unchecked
            {
                var h = 2166136261u;
                foreach (var c in id) h = (h ^ c) * 16777619u;
                return (h % 1000) / 1000f;
            }
        }
    }
}
