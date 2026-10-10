using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.City;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Relics;
using Codigames.Game.Fog;
using Codigames.Game.Lairs;
using Codigames.Game.Map;
using Codigames.Game.UI.Relics;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Relics;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using TMPro;
using UnityEngine;
using VContainer.Unity;
using Vector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Relics
{
    // The relics on the map (the web's Passes 1.1 spell zones, 3a-ii bursts, 3c relics, 3d aura badges, 4.6 bubbles):
    // - every Shrine holding a relic shows it: awake, floating over the roof in a halo, bobbing, its window a thin arc
    //   round it sweeping off from twelve; asleep, small and dim on the altar, under a parchment bubble with its Zs — a
    //   tap on the bubble opens the Shrine's card;
    // - an awake aura is enchanted ground: a violet floor breathing, a haze bleeding cell into cell, a sigil on one cell
    //   in seven, a mote rising from one in three, its outside traced bright, and a wave breathing out from the Shrine;
    // - a building relic tags every roof its aura pays (+10%);
    // - a relic woken sweeps a bright ring out over its aura, and floats what it does and the Mana it took.
    public class ShrinesView : IStartable, ITickable, IDisposable
    {
        private const int FILL_ORDER = -45;
        private const int HAZE_ORDER = -44;
        private const int SIGIL_ORDER = -43;
        private const int BORDER_ORDER = -42;
        private const int MOTE_ORDER = 108;
        private const int RELIC_GLOW_ORDER = 110;
        private const int RELIC_ORDER = 111;
        private const int ARC_ORDER = 112;
        private const int BADGE_ORDER = 690;
        private const int FLOAT_ORDER = 800;
        private const float PULSE_SECONDS = 4f;
        private const float WAVE_SECONDS = 2.6f;
        private const float MOTE_SECONDS = 3.2f;
        private const float BURST_SECONDS = 1.5f;
        private const float FLOAT_SECONDS = 1.6f;
        private static readonly Color FILL = new(124 / 255f, 84 / 255f, 214 / 255f, 0.15f);
        private static readonly Color BORDER = new(198 / 255f, 164 / 255f, 1f, 0.95f);
        private static readonly Color GLOW = new(214 / 255f, 190 / 255f, 1f, 0.85f);
        private static readonly Color DIAL = new(28 / 255f, 16 / 255f, 48 / 255f, 0.55f);
        private static readonly Color PARCHMENT = new Color32(0xf6, 0xe8, 0xc6, 0xff);
        private static readonly Color INK = new Color32(0x3b, 0x24, 0x12, 0xff);
        private static readonly Color ASLEEP = new(0.7f, 0.68f, 0.64f, 0.85f);

        private readonly Shrines _shrines;
        private readonly Kingdom.Relics.Relics _relics;
        private readonly RelicCards _cards;
        private readonly RelicActions _actions;
        private readonly RelicWords _words;
        private readonly CityState _city;
        private readonly CityView _cityView;
        private readonly ProvinceMap _map;
        private readonly BuildingCollection _buildings;
        private readonly SightArt _sight;
        private readonly RelicArtAsset _art;
        private readonly LairBubbleView _bubblePrefab;
        private readonly Kingdom.Economy.Stores _stores;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;

        private readonly Dictionary<string, Held> _held = new();
        private readonly Dictionary<string, Aura> _auras = new();
        private readonly List<TextMeshPro> _badges = new();
        private readonly List<Burst> _bursts = new();
        private readonly List<Floater> _floaters = new();

        private Transform _root;

        public ShrinesView(Shrines shrines, Kingdom.Relics.Relics relics, RelicCards cards, RelicActions actions, RelicWords words, CityState city,
            CityView cityView, ProvinceMap map, BuildingCollection buildings, SightArt sight, RelicArtAsset art, LairBubbleView bubblePrefab,
            Kingdom.Economy.Stores stores, IClock clock, NumberFormat numbers, Localizer localizer)
        {
            _shrines = shrines;
            _relics = relics;
            _cards = cards;
            _actions = actions;
            _words = words;
            _city = city;
            _cityView = cityView;
            _map = map;
            _buildings = buildings;
            _sight = sight;
            _art = art;
            _bubblePrefab = bubblePrefab;
            _stores = stores;
            _clock = clock;
            _numbers = numbers;
            _localizer = localizer;
        }

        private float Cell => _map.Grid.cellSize.x;

        public void Start()
        {
            _root = new GameObject("Shrines").transform;
            _actions.Activated += OnActivated;
        }

        public void Dispose()
        {
            _actions.Activated -= OnActivated;
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
        }

        // The Shrine whose sleeping bubble covers this world point, or null: a tap there opens its card.
        public string ShrineAt(Vector3 world)
        {
            foreach (var (id, held) in _held)
                if (held.Bubble != null && held.Bubble.gameObject.activeSelf && Contains(held.Bubble.Bounds, world)) return id;
            return null;
        }

        private static bool Contains(Bounds bounds, Vector3 world)
            => world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y;

        public void Tick()
        {
            if (_root == null) return;
            var now = _clock.NowMs;
            var time = Time.unscaledTime;
            var hosting = new HashSet<string>();
            var awake = new HashSet<string>();
            foreach (var shrine in _shrines.All)
            {
                var relic = _shrines.Hosted(shrine);
                var view = _cityView.ViewOf(shrine.Id);
                if (relic == null || view == null) continue;
                hosting.Add(shrine.Id);
                var held = HeldOf(shrine.Id);
                var isAwake = _shrines.IsAwake(relic);
                ShowRelic(held, shrine, relic, view.ArtBounds, isAwake, now, time);
                if (!isAwake) continue;
                awake.Add(shrine.Id);
                ShowAura(shrine, relic, time);
            }

            foreach (var id in _held.Keys.Where(k => !hosting.Contains(k)).ToList()) HideHeld(id);
            foreach (var id in _auras.Keys.Where(k => !awake.Contains(k)).ToList()) HideAura(id);
            ShowBadges();
            StepBursts(time);
            StepFloaters(time);
        }

        // ---- the relic in its Shrine, and its bubble asleep

        private sealed class Held
        {
            public SpriteRenderer Relic;
            public SpriteRenderer Glow;
            public MeshFilter Arc;
            public LairBubbleView Bubble;
        }

        private Held HeldOf(string shrineId)
        {
            if (_held.TryGetValue(shrineId, out var held)) return held;
            held = new Held
            {
                Glow = Sprite("Halo", RELIC_GLOW_ORDER, _art.Halo),
                Relic = Sprite("Relic", RELIC_ORDER, null),
                Arc = Mesh("Window", ARC_ORDER),
                Bubble = UnityEngine.Object.Instantiate(_bubblePrefab, _root),
            };
            held.Bubble.name = shrineId + " bubble";
            held.Bubble.Restyle(_art.Bubble, _art.BubbleTail, INK, false);
            held.Bubble.gameObject.SetActive(false);
            _held[shrineId] = held;
            return held;
        }

        private void ShowRelic(Held held, DistrictState shrine, string relic, Bounds art, bool awake, double now, float time)
        {
            var sprite = _cards.Get(relic).Icon;
            var plot = art.size.x;
            held.Relic.gameObject.SetActive(true);
            held.Relic.sprite = sprite;
            if (awake)
            {
                held.Bubble.Hide();
                var s = plot * 0.5f;
                var bob = Mathf.Sin(time / 0.7f) * s * 0.06f;
                var centre = new Vector3(art.center.x, art.max.y - art.size.y * 0.2f + s * 0.45f + bob, 0);
                Fit(held.Relic, sprite, centre, s);
                held.Relic.color = Color.white;
                held.Glow.gameObject.SetActive(true);
                Fit(held.Glow, _art.Halo, centre, s * 1.3f);
                held.Glow.color = new Color(GLOW.r, GLOW.g, GLOW.b, 0.7f);
                // Its window: a dark dial, and the part left in light from twelve.
                var window = _relics.WindowMs(relic, _relics.Level(relic));
                var left = window > 0 ? (float)(Math.Max(0, (_shrines.WindowEndsAt(relic) ?? now) - now) / window) : 0;
                held.Arc.gameObject.SetActive(true);
                ArcMesh(held.Arc.sharedMesh, centre, s * 0.62f, Mathf.Max(0.03f * Cell, s * 0.07f), left);
                return;
            }

            held.Glow.gameObject.SetActive(false);
            held.Arc.gameObject.SetActive(false);
            var small = plot * 0.3f;
            Fit(held.Relic, sprite, new Vector3(art.center.x, art.min.y + art.size.y * 0.42f, 0), small);
            held.Relic.color = ASLEEP;
            var tip = new Vector3(art.center.x, art.max.y - art.size.y * 0.3f, 0);
            held.Bubble.Show(sprite, "Zz", tip, Mathf.Clamp(plot * 0.32f, 0.35f * Cell, 0.6f * Cell) / 1.6f, Mathf.Repeat(shrine.Anchor.X * 0.37f, 1));
        }

        private void HideHeld(string shrineId)
        {
            var held = _held[shrineId];
            held.Relic.gameObject.SetActive(false);
            held.Glow.gameObject.SetActive(false);
            held.Arc.gameObject.SetActive(false);
            held.Bubble.Hide();
        }

        // ---- the aura: enchanted ground

        private sealed class Aura
        {
            public string Key;
            public Transform Root;
            public MeshFilter Fill;
            public MeshFilter Border;
            public MeshFilter Wave;
            public Color[] FillColours;
            public Color[] Breathing;
            public readonly List<SpriteRenderer> Haze = new();
            public readonly List<(SpriteRenderer Mote, Vector3 From, float Seed)> Motes = new();
            public Vector3 Centre;
            public float Reach;
        }

        private IReadOnlyList<Vector2Int> AuraCells(DistrictState shrine, string relic)
        {
            var building = _buildings.Get<BuildingAsset>(shrine.DefinitionId);
            var r = _shrines.Radius(relic);
            var cells = new List<Vector2Int>();
            for (var x = shrine.Anchor.X - r; x < shrine.Anchor.X + building.Width + r; x++)
            for (var y = shrine.Anchor.Y - r; y < shrine.Anchor.Y + building.Height + r; y++)
            {
                var cell = new Vector2Int(x, y);
                if (_map.Contains(cell) && _shrines.Covers(shrine, relic, cell)) cells.Add(cell);
            }

            return cells;
        }

        private void ShowAura(DistrictState shrine, string relic, float time)
        {
            var cells = AuraCells(shrine, relic);
            var key = relic + "@" + shrine.Anchor.X + "," + shrine.Anchor.Y + "#" + cells.Count;
            if (!_auras.TryGetValue(shrine.Id, out var aura) || aura.Key != key)
            {
                if (aura != null) UnityEngine.Object.Destroy(aura.Root.gameObject);
                aura = MakeAura(shrine, cells, key);
                _auras[shrine.Id] = aura;
            }

            // The floor and the haze breathe; the motes rise; the wave leaves the Shrine and fades at the edge.
            var pulse = 0.82f + 0.18f * Mathf.Sin(time / PULSE_SECONDS * 2 * Mathf.PI);
            for (var i = 0; i < aura.FillColours.Length; i++)
            {
                var c = aura.FillColours[i];
                aura.Breathing[i] = new Color(c.r, c.g, c.b, c.a * pulse);
            }

            aura.Fill.sharedMesh.colors = aura.Breathing;
            foreach (var haze in aura.Haze) haze.color = new Color(GLOW.r, GLOW.g, GLOW.b, 0.2f * pulse);
            foreach (var (mote, from, seed) in aura.Motes)
            {
                var t = Mathf.Repeat(time / MOTE_SECONDS + seed, 1);
                mote.transform.position = from + new Vector3(0, t * Cell * 0.5f, 0);
                mote.color = new Color(GLOW.r, GLOW.g, GLOW.b, pulse * Mathf.Sin(t * Mathf.PI) * 0.75f);
            }

            var w = Mathf.Repeat(time / WAVE_SECONDS, 1);
            var rx = aura.Reach * (0.08f + 0.92f * w);
            var ry = rx / 2;
            var c0 = aura.Centre;
            var top = c0 + new Vector3(0, ry, 0);
            var right = c0 + new Vector3(rx, 0, 0);
            var bottom = c0 + new Vector3(0, -ry, 0);
            var left = c0 + new Vector3(-rx, 0, 0);
            var colour = new Color(GLOW.r, GLOW.g, GLOW.b, 0.35f + 0.6f * (1 - w));
            GlowLines.Build(aura.Wave.sharedMesh, new[] { (top, right), (right, bottom), (bottom, left), (left, top) },
                new[] { (Cell * 0.1f * (1 - w * 0.5f), colour) });
        }

        private Aura MakeAura(DistrictState shrine, IReadOnlyList<Vector2Int> cells, string key)
        {
            var root = new GameObject("Aura " + shrine.Id).transform;
            root.SetParent(_root, false);
            var aura = new Aura { Key = key, Root = root };
            var set = new HashSet<Vector2Int>(cells);

            // The floor: every cell's diamond.
            var vertices = new List<Vector3>();
            var colours = new List<Color>();
            var triangles = new List<int>();
            foreach (var cell in cells)
            {
                var corners = ProvinceGeometry.Corners(_map, cell, 1, 1);
                var start = vertices.Count;
                vertices.AddRange(corners);
                for (var i = 0; i < 4; i++) colours.Add(FILL);
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            aura.Fill = Mesh("Floor", FILL_ORDER, root);
            var fill = aura.Fill.sharedMesh;
            fill.SetVertices(vertices);
            fill.SetColors(colours);
            fill.SetTriangles(triangles, 0);
            aura.FillColours = colours.ToArray();
            aura.Breathing = new Color[colours.Count];

            // Its outside, traced bright — never the grid inside it.
            var edges = new List<(Vector3, Vector3)>();
            foreach (var cell in cells)
            {
                var corners = ProvinceGeometry.Corners(_map, cell, 1, 1);
                foreach (var neighbour in Modules.Grid.GridMath.Neighbours(cell))
                {
                    if (set.Contains(neighbour)) continue;
                    edges.Add(SharedEdge(corners, cell, neighbour));
                }
            }

            aura.Border = Mesh("Border", BORDER_ORDER, root);
            GlowLines.Build(aura.Border.sharedMesh, edges, new[] { (Cell * 0.09f, new Color(GLOW.r, GLOW.g, GLOW.b, 0.35f)), (Cell * 0.04f, BORDER) });
            aura.Wave = Mesh("Wave", BORDER_ORDER, root);

            // The haze, a sigil on one cell in seven, a mote on one in three — hashed on the cell, so they hold still.
            foreach (var cell in cells)
            {
                var centre = _map.CellCentre(cell);
                var haze = Sprite("Haze", HAZE_ORDER, _art.SpellGlow, root);
                Fit(haze, _art.SpellGlow, centre, Cell * 1.2f);
                aura.Haze.Add(haze);
                var seed = (uint)((cell.X * 374761393) ^ (cell.Y * 668265263));
                if (seed % 7 == 0)
                {
                    var sigil = Sprite("Sigil", SIGIL_ORDER, _art.SpellSigil, root);
                    Fit(sigil, _art.SpellSigil, centre, Cell * 0.4f);
                    sigil.color = new Color(1, 1, 1, 0.4f);
                }
                else if (seed % 3 == 1)
                {
                    var mote = Sprite("Mote", MOTE_ORDER, _art.SpellMote, root);
                    Fit(mote, _art.SpellMote, centre, Cell * 0.12f);
                    var hash = (uint)((cell.X * 73856093) ^ (cell.Y * 19349663));
                    var from = centre + new Vector3(((hash >> 10) % 100 / 100f - 0.5f) * Cell * 0.5f, 0, 0);
                    aura.Motes.Add((mote, from, hash % 1000 / 1000f));
                }
            }

            var building = _buildings.Get<BuildingAsset>(shrine.DefinitionId);
            var footprint = ProvinceGeometry.Corners(_map, shrine.Anchor, building.Width, building.Height);
            aura.Centre = (footprint[0] + footprint[1] + footprint[2] + footprint[3]) / 4;
            aura.Reach = cells.Max(c => Mathf.Max(Mathf.Abs(_map.CellCentre(c).x - aura.Centre.x) + Cell / 2,
                (Mathf.Abs(_map.CellCentre(c).y - aura.Centre.y) + _map.Grid.cellSize.y / 2) * 2));
            return aura;
        }

        private void HideAura(string shrineId)
        {
            UnityEngine.Object.Destroy(_auras[shrineId].Root.gameObject);
            _auras.Remove(shrineId);
        }

        private (Vector3, Vector3) SharedEdge(Vector3[] corners, Vector2Int cell, Vector2Int neighbour)
        {
            var between = (_map.CellCentre(cell) + _map.CellCentre(neighbour)) / 2f;
            var best = (corners[0], corners[1]);
            var bestDistance = float.MaxValue;
            for (var i = 0; i < 4; i++)
            {
                var a = corners[i];
                var b = corners[(i + 1) % 4];
                var distance = Vector3.Distance((a + b) / 2f, between);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = (a, b);
            }

            return best;
        }

        // ---- what an awake aura pays, over each roof it reaches

        private void ShowBadges()
        {
            var used = 0;
            foreach (var shrine in _shrines.All)
            {
                var relic = _shrines.Hosted(shrine);
                if (relic == null || !_shrines.IsAwake(relic)) continue;
                var stats = _cards.Get(relic).Stats.Select(s => s.Stat).ToList();
                var text = "+" + _words.Percent(_relics.Value(relic));
                foreach (var district in _city.Districts)
                {
                    if (!district.Built || district == shrine || !Reaches(stats, district) || !InAura(shrine, relic, district)) continue;
                    var view = _cityView.ViewOf(district.Id);
                    if (view == null) continue;
                    if (used == _badges.Count) _badges.Add(Badge());
                    var badge = _badges[used++];
                    badge.gameObject.SetActive(true);
                    badge.text = text;
                    var art = view.ArtBounds;
                    badge.transform.position = new Vector3(art.center.x, art.max.y - art.size.y * 0.18f, 0);
                }
            }

            for (var i = used; i < _badges.Count; i++) _badges[i].gameObject.SetActive(false);
        }

        // A building relic reaches a house with people in it, a building with a crew, a hall that trains.
        private bool Reaches(IReadOnlyList<string> stats, DistrictState district)
        {
            var production = _buildings.Get(district.DefinitionId).Production;
            return stats.Any(stat => stat switch
            {
                RelicStats.TAX_RATE => _stores.Residents(district) > 0,
                RelicStats.WORKER_STRIKE_SPEED or RelicStats.WORKER_SPEED => production.MaxWorkersPerLevel.Count > 0,
                RelicStats.TRAINING_SPEED => production.Trains.Count > 0,
                _ => false,
            });
        }

        private bool InAura(DistrictState shrine, string relic, DistrictState district)
        {
            var building = _buildings.Get(district.DefinitionId);
            for (var dx = 0; dx < building.Width; dx++)
            for (var dy = 0; dy < building.Height; dy++)
                if (_shrines.Covers(shrine, relic, new Vector2Int(district.Anchor.X + dx, district.Anchor.Y + dy))) return true;
            return false;
        }

        private TextMeshPro Badge()
        {
            var go = new GameObject("Badge");
            go.transform.SetParent(_root, false);
            var text = go.AddComponent<TextMeshPro>();
            text.fontSize = 1.4f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = INK;
            text.fontStyle = FontStyles.Bold;
            text.outlineWidth = 0.25f;
            text.outlineColor = PARCHMENT;
            text.sortingOrder = BADGE_ORDER;
            text.rectTransform.sizeDelta = new Vector2(3, 1);
            return text;
        }

        // ---- a relic woken

        private sealed class Burst
        {
            public MeshFilter Ring;
            public Vector3 Centre;
            public float Reach;
            public float Start;
        }

        private sealed class Floater
        {
            public TextMeshPro Text;
            public Vector3 From;
            public float Start;
        }

        private void OnActivated(string relic)
        {
            var shrine = _shrines.HostOf(relic);
            if (shrine == null) return;
            var building = _buildings.Get<BuildingAsset>(shrine.DefinitionId);
            var corners = ProvinceGeometry.Corners(_map, shrine.Anchor, building.Width, building.Height);
            var centre = (corners[0] + corners[1] + corners[2] + corners[3]) / 4;
            var reach = (_shrines.Radius(relic) + building.Width / 2f) * Cell;
            _bursts.Add(new Burst { Ring = Mesh("Burst", BORDER_ORDER + 1), Centre = centre, Reach = reach, Start = Time.unscaledTime });
            var window = _numbers.Duration(Math.Round(_relics.WindowMs(relic, _relics.Level(relic)) / 1000));
            Float(_words.Short(_cards.Get(relic), _relics.Level(relic)) + " · " + window, centre + new Vector3(0, Cell * 0.6f, 0), 0);
            Float("−" + _numbers.Exact(_shrines.ActivationCost(relic)) + " <sprite name=\"Mana\">", centre + new Vector3(0, Cell * 0.2f, 0), 0.15f);
        }

        private void StepBursts(float time)
        {
            for (var i = _bursts.Count - 1; i >= 0; i--)
            {
                var burst = _bursts[i];
                var t = (time - burst.Start) / BURST_SECONDS;
                if (t >= 1)
                {
                    UnityEngine.Object.Destroy(burst.Ring.gameObject);
                    _bursts.RemoveAt(i);
                    continue;
                }

                var ease = 1 - Mathf.Pow(1 - t, 3);
                Ellipse(burst.Ring.sharedMesh, burst.Centre, Mathf.Max(0.01f, burst.Reach * ease), Cell * 0.12f * (1 - t),
                    new Color(GLOW.r, GLOW.g, GLOW.b, (1 - t) * 0.9f));
            }
        }

        private void Float(string text, Vector3 at, float delay)
        {
            var go = new GameObject("Floater");
            go.transform.SetParent(_root, false);
            var label = go.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = 1.6f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.outlineWidth = 0.22f;
            label.outlineColor = new Color32(40, 22, 10, 220);
            label.sortingOrder = FLOAT_ORDER;
            label.rectTransform.sizeDelta = new Vector2(8, 1.2f);
            label.alpha = 0;
            go.transform.position = at;
            _floaters.Add(new Floater { Text = label, From = at, Start = Time.unscaledTime + delay });
        }

        private void StepFloaters(float time)
        {
            for (var i = _floaters.Count - 1; i >= 0; i--)
            {
                var floater = _floaters[i];
                var t = (time - floater.Start) / FLOAT_SECONDS;
                if (t < 0) continue;
                if (t >= 1)
                {
                    UnityEngine.Object.Destroy(floater.Text.gameObject);
                    _floaters.RemoveAt(i);
                    continue;
                }

                floater.Text.alpha = 1 - t;
                floater.Text.transform.position = floater.From + new Vector3(0, t * Cell * 0.5f, 0);
            }
        }

        // ---- meshes and sprites

        private void ArcMesh(Mesh mesh, Vector3 centre, float radius, float width, float left)
        {
            const int segments = 48;
            var edges = new List<(Vector3, Vector3)>();
            var lit = new List<(Vector3, Vector3)>();
            for (var i = 0; i < segments; i++)
            {
                var a0 = Mathf.PI / 2 - 2 * Mathf.PI * i / segments;
                var a1 = Mathf.PI / 2 - 2 * Mathf.PI * (i + 1) / segments;
                var seg = (centre + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * radius, centre + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * radius);
                edges.Add(seg);
                if ((i + 1) / (float)segments <= left + 0.0001f) lit.Add(seg);
            }

            var dial = new Mesh();
            GlowLines.Build(dial, edges, new[] { (width, DIAL) });
            var light = new Mesh();
            GlowLines.Build(light, lit, new[] { (width * 1.6f, new Color(GLOW.r, GLOW.g, GLOW.b, 0.35f)), (width, GLOW) });
            mesh.Clear();
            mesh.CombineMeshes(new[]
            {
                new CombineInstance { mesh = dial, transform = Matrix4x4.identity },
                new CombineInstance { mesh = light, transform = Matrix4x4.identity },
            }, true, false);
            UnityEngine.Object.Destroy(dial);
            UnityEngine.Object.Destroy(light);
        }

        // The ground is a diamond on screen, so a ring on it is an ellipse half as tall.
        private static void Ellipse(Mesh mesh, Vector3 centre, float rx, float width, Color colour)
        {
            const int segments = 64;
            var edges = new List<(Vector3, Vector3)>();
            for (var i = 0; i < segments; i++)
            {
                var a0 = 2 * Mathf.PI * i / segments;
                var a1 = 2 * Mathf.PI * (i + 1) / segments;
                edges.Add((centre + new Vector3(Mathf.Cos(a0) * rx, Mathf.Sin(a0) * rx * 0.5f), centre + new Vector3(Mathf.Cos(a1) * rx, Mathf.Sin(a1) * rx * 0.5f)));
            }

            GlowLines.Build(mesh, edges, new[] { (width * 2.2f, new Color(colour.r, colour.g, colour.b, colour.a * 0.35f)), (width, colour) });
        }

        private static void Fit(SpriteRenderer renderer, Sprite sprite, Vector3 centre, float size)
        {
            renderer.sprite = sprite;
            renderer.transform.position = centre;
            if (sprite == null) return;
            var side = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            renderer.transform.localScale = Vector3.one * (size / side);
        }

        private SpriteRenderer Sprite(string name, int order, Sprite sprite, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ?? _root, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private MeshFilter Mesh(string name, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ?? _root, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = new UnityEngine.Mesh { name = name };
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _sight.Border;
            renderer.sortingOrder = order;
            return filter;
        }
    }
}
