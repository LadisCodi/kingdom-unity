using System.Collections.Generic;
using Codigames.Game.Data.City;
using Codigames.Game.Map;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using UnityEngine;
using VContainer;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Crews
{
    // The villagers no crew has taken, strolling round the buildings that house them (the web's villagers.ts): each
    // walks to an open cell near its home, stands a while, and walks on. Cosmetic only: nothing here is the sim's,
    // and the flock starts afresh on every load. Its numbers are deliberately not balance data.
    public class VillagersView : MonoBehaviour
    {
        private const int WANDER_RADIUS = 2;
        private const float SPEED_MIN = 0.3f;
        private const float SPEED_MAX = 0.6f;
        private const float PAUSE_MIN = 0.8f;
        private const float PAUSE_MAX = 3.5f;
        private static readonly string[] CAST = { "villager_1", "villager_2", "villager_3", "villager_4" };

        [SerializeField] private CharacterCatalog _characters;
        [SerializeField] private SpriteRenderer _memberPrefab;

        private sealed class Agent
        {
            public string Home;
            public Vector2 From;
            public Vector2 To;
            public float LegStart;
            public float LegEnd;
            public float PauseUntil;
            public int Phase;
            public SpriteRenderer Renderer;
        }

        private readonly List<Agent> _agents = new();
        private readonly List<DistrictState> _homes = new();
        private readonly List<ModuleVector2Int> _open = new();

        private CityState _city;
        private Codigames.Kingdom.Crews.Workforce _crews;
        private Stores _stores;
        private IConstructionSettings _construction;
        private BuildingCollection _buildings;
        private FogOfWar _fog;
        private GroundState _ground;
        private Codigames.Kingdom.Map.IProvinceMap _province;
        private ProvinceMap _map;
        private Vector3 _origin;
        private Vector3 _alongX;
        private Vector3 _alongY;

        [Inject]
        public void Construct(CityState city, Codigames.Kingdom.Crews.Workforce crews, Stores stores, IConstructionSettings construction,
            BuildingCollection buildings, FogOfWar fog, GroundState ground, Codigames.Kingdom.Map.IProvinceMap province, ProvinceMap map)
        {
            _city = city;
            _crews = crews;
            _stores = stores;
            _construction = construction;
            _buildings = buildings;
            _fog = fog;
            _ground = ground;
            _province = province;
            _map = map;
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

            _homes.Clear();
            foreach (var district in _city.Districts)
                if (district.Built && (district.DefinitionId == _construction.Townhall.Id || _stores.HousingOf(district) > 0)) _homes.Add(district);
            var idle = _homes.Count == 0 ? 0 : _crews.FreeVillagers;

            // The flock follows the idle head-count; newcomers spread over the homes.
            while (_agents.Count > idle) Drop(_agents.Count - 1);
            var now = Time.time;
            while (_agents.Count < idle)
            {
                var home = _homes[_agents.Count % _homes.Count];
                var at = Target(home);
                _agents.Add(new Agent
                {
                    Home = home.Id, From = at, To = at, LegStart = now, LegEnd = now, PauseUntil = now + Random.Range(0f, PAUSE_MAX),
                    Phase = Random.Range(0, 997), Renderer = Instantiate(_memberPrefab, transform),
                });
            }

            foreach (var agent in _agents) Step(agent, now);
        }

        private void Step(Agent agent, float now)
        {
            var home = HomeOf(agent.Home);
            if (home == null)
            {
                // Its building is gone: it moves in somewhere else.
                home = _homes[Random.Range(0, _homes.Count)];
                agent.Home = home.Id;
                agent.PauseUntil = agent.LegEnd = now;
            }

            if (now >= agent.LegEnd && now >= agent.PauseUntil)
            {
                agent.From = agent.To;
                agent.To = Target(home);
                agent.LegStart = now;
                agent.LegEnd = now + Vector2.Distance(agent.From, agent.To) / Random.Range(SPEED_MIN, SPEED_MAX);
                agent.PauseUntil = agent.LegEnd + Random.Range(PAUSE_MIN, PAUSE_MAX);
            }

            var walking = now < agent.LegEnd;
            var at = walking ? Vector2.Lerp(agent.From, agent.To, (now - agent.LegStart) / (agent.LegEnd - agent.LegStart)) : agent.To;
            var renderer = agent.Renderer;
            renderer.transform.position = _origin + _alongX * at.x + _alongY * at.y;
            // Facing across the screen, not the grid: a step in +y goes left on screen.
            if (walking) renderer.flipX = (agent.To.x - agent.From.x) - (agent.To.y - agent.From.y) > 0f;
            var cast = CAST[agent.Phase % CAST.Length];
            renderer.sprite = _characters.Frame(cast, walking ? Pose.Walk : Pose.Idle, now + agent.Phase / 100f);
        }

        // An open cell ringing the home (1 to 2 away): land, revealed, bare, unbuilt; else its doorstep. A little off
        // the cell's middle, so the flock does not line up.
        private Vector2 Target(DistrictState home)
        {
            var building = _buildings.Get(home.DefinitionId);
            var x1 = home.Anchor.X + building.Width - 1;
            var y1 = home.Anchor.Y + building.Height - 1;
            _open.Clear();
            for (var y = home.Anchor.Y - WANDER_RADIUS; y <= y1 + WANDER_RADIUS; y++)
            for (var x = home.Anchor.X - WANDER_RADIUS; x <= x1 + WANDER_RADIUS; x++)
            {
                var d = Mathf.Max(Mathf.Max(home.Anchor.X - x, x - x1), Mathf.Max(home.Anchor.Y - y, y - y1));
                if (d < 1) continue;
                var cell = new ModuleVector2Int(x, y);
                var terrain = _province.TerrainAt(cell);
                if (terrain == null || terrain == "Water" || !_fog.IsRevealed(cell) || _ground.Features.ContainsKey(cell)) continue;
                if (CityQueries.At(_city, _buildings, cell) != null) continue;
                _open.Add(cell);
            }

            if (_open.Count == 0)
                for (var y = home.Anchor.Y; y <= y1; y++)
                for (var x = home.Anchor.X; x <= x1; x++)
                    _open.Add(new ModuleVector2Int(x, y));

            var pick = _open[Random.Range(0, _open.Count)];
            return new Vector2(pick.X + Random.Range(0f, 0.35f), pick.Y + Random.Range(0f, 0.35f));
        }

        private DistrictState HomeOf(string id)
        {
            foreach (var home in _homes)
                if (home.Id == id) return home;
            return null;
        }

        private void Drop(int index)
        {
            Destroy(_agents[index].Renderer.gameObject);
            _agents.RemoveAt(index);
        }
    }
}
