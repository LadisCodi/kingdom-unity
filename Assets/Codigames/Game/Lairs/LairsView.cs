using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data.Sites;
using Codigames.Game.Fog;
using Codigames.Game.Map;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Lairs;
using Codigames.Modules.Clock;
using Codigames.Modules.Grid;
using Codigames.Modules.Localization;
using UnityEngine;
using VContainer.Unity;
using Vector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Lairs
{
    // The lairs on the map (Docs/features/18-garrisons-and-raids.md §3, §6): the ground the standing ones hold — a red
    // tint on each cell of the union of their zones, stronger under fog, and a border round the union's outside, solid
    // where the cell inside is revealed and dashed where it is fog — each lair's model once found, whatever the fog on
    // its own plot, and over it the warning bubble with its creature and the time to its next raid.
    public class LairsView : IStartable, ITickable, IDisposable
    {
        private const int ZONE_ORDER = -10;
        private const int BORDER_ORDER = -9;
        private const int MODEL_ORDER = 100;
        // The web's measures are shares of a cell's diamond height, half a world unit: the line 0.035 of it, the bubble's
        // body 0.62, its tip sunk 0.1 into the art.
        private const float CELL = 0.5f;
        private const float BORDER = 0.035f * CELL;
        private const float BUBBLE = 0.62f * CELL;
        private const float SINK = 0.1f * CELL;
        private const float BODY = 1.6f;
        private static readonly Color TINT = new Color32(150, 30, 20, 71);
        private static readonly Color TINT_FOG = new Color32(170, 40, 25, 97);
        private static readonly Color EDGE = new Color32(0xb3, 0x40, 0x2c, 0xff);

        private readonly Kingdom.Lairs.Lairs _lairs;
        private readonly LairGround _ground;
        private readonly ProvinceSitesAsset _sites;
        private readonly FogOfWar _fog;
        private readonly ProvinceMap _map;
        private readonly Kingdom.Map.IProvinceMap _province;
        private readonly SightArt _art;
        private readonly LairBubbleView _bubblePrefab;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Dictionary<string, SpriteRenderer> _models = new();
        private readonly Dictionary<string, LairBubbleView> _bubbles = new();

        private Transform _parent;
        private MeshFilter _zone;
        private MeshFilter _border;

        public LairsView(Kingdom.Lairs.Lairs lairs, LairGround ground, ProvinceSitesAsset sites, FogOfWar fog, ProvinceMap map,
            Kingdom.Map.IProvinceMap province, SightArt art, LairBubbleView bubblePrefab, IClock clock, NumberFormat numbers)
        {
            _lairs = lairs;
            _ground = ground;
            _sites = sites;
            _fog = fog;
            _map = map;
            _province = province;
            _art = art;
            _bubblePrefab = bubblePrefab;
            _clock = clock;
            _numbers = numbers;
        }

        public void Start()
        {
            _parent = new GameObject("Lairs").transform;
            _zone = Mesh("Zone", ZONE_ORDER);
            _border = Mesh("ZoneBorder", BORDER_ORDER);
            Refresh();
            _fog.Changed += OnFogChanged;
            _lairs.Armed += OnArmed;
            _lairs.Changed += OnChanged;
        }

        public void Dispose()
        {
            _fog.Changed -= OnFogChanged;
            _lairs.Armed -= OnArmed;
            _lairs.Changed -= OnChanged;
        }

        // The standing lair whose model or bubble is under a world point; null when none is.
        public string LairAt(Vector3 world)
        {
            foreach (var (id, bubble) in _bubbles)
                if (bubble.gameObject.activeSelf && Contains(bubble.Bounds, world)) return id;
            foreach (var (id, model) in _models)
                if (model.gameObject.activeSelf && Contains(model.bounds, world) && Solid(model, world)) return id;
            return null;
        }

        public void Tick()
        {
            var now = _clock.NowMs;
            foreach (var site in _lairs.All)
            {
                if (!_bubbles.TryGetValue(site.Id, out var bubble) || !bubble.gameObject.activeSelf) continue;
                var at = _lairs.StateOf(site.Id)?.NextRaidAt;
                bubble.SetText(at == null ? "–" : Compact(at.Value - now));
            }
        }

        private void OnFogChanged(IReadOnlyCollection<Vector2Int> cells) => Refresh();

        private void OnArmed(string id, double at) => Refresh();

        private void OnChanged(string id) => Refresh();

        private void Refresh()
        {
            DrawZone();
            var now = _clock.NowMs;
            foreach (var site in _lairs.All)
            {
                var state = _lairs.StateOf(site.Id);
                var standing = state is { Cleared: false };
                var model = Model(site);
                model.gameObject.SetActive(standing);
                var bubble = Bubble(site);
                // Beaten, the reward waits: no clock to warn of.
                if (!standing || state.Defeated)
                {
                    bubble.Hide();
                    continue;
                }

                var scale = BUBBLE / BODY;
                var top = model.bounds.max.y - Ink(model) - SINK;
                var tip = new Vector3(model.transform.position.x, top, 0);
                var phase = Mathf.Abs(site.Id.GetHashCode() % 100) / 100f;
                bubble.Show(_sites.LairOf(site.Id)?.Medal, state.NextRaidAt == null ? "–" : Compact(state.NextRaidAt.Value - now), tip, scale, phase);
            }
        }

        private void DrawZone()
        {
            var held = new HashSet<Vector2Int>();
            foreach (var site in _lairs.All.Where(l => _ground.Holds(l.Id)))
                foreach (var cell in GridMath.AroundRect(site.Anchor, site.Size, site.Size, site.Radius))
                    if (_province.Contains(cell)) held.Add(cell);

            var vertices = new List<Vector3>();
            var colours = new List<Color>();
            var triangles = new List<int>();
            var solid = new List<(Vector3, Vector3)>();
            var dashed = new List<(Vector3, Vector3)>();
            foreach (var cell in held)
            {
                var clear = _fog.IsRevealed(cell);
                var corners = ProvinceGeometry.Corners(_map, cell, 1, 1);
                var start = vertices.Count;
                foreach (var corner in corners)
                {
                    vertices.Add(corner);
                    // A mesh's colours are not converted to the linear space it is drawn in: they are given in it.
                    colours.Add((clear ? TINT : TINT_FOG).linear);
                }

                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                foreach (var neighbour in GridMath.Neighbours(cell))
                {
                    if (held.Contains(neighbour)) continue;
                    var edge = SharedEdge(cell, neighbour);
                    if (clear) solid.Add(edge);
                    else dashed.AddRange(Dashes(edge));
                }
            }

            var zone = _zone.sharedMesh ??= new Mesh { name = "LairZone" };
            zone.Clear();
            zone.SetVertices(vertices);
            zone.SetColors(colours);
            zone.SetTriangles(triangles, 0);
            var border = _border.sharedMesh ??= new Mesh { name = "LairBorder" };
            GlowLines.Build(border, solid.Concat(dashed).ToList(), new[] { (BORDER, EDGE.linear) });
        }

        // The reach line's dash, shares of the diamond's height: 0.18 on, 0.12 off.
        private static IEnumerable<(Vector3, Vector3)> Dashes((Vector3 A, Vector3 B) edge)
        {
            var length = Vector3.Distance(edge.A, edge.B);
            var along = (edge.B - edge.A) / length;
            for (var s = 0f; s < length; s += 0.3f * CELL)
                yield return (edge.A + along * s, edge.A + along * Mathf.Min(length, s + 0.18f * CELL));
        }

        // The side of a cell's diamond that faces a neighbour: the one whose middle is nearest theirs.
        private (Vector3, Vector3) SharedEdge(Vector2Int cell, Vector2Int neighbour)
        {
            var corners = ProvinceGeometry.Corners(_map, cell, 1, 1);
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

        // A feature's two plots across its footprint, feet on its bottom corner.
        private SpriteRenderer Model(ILairSite site)
        {
            if (_models.TryGetValue(site.Id, out var model)) return model;
            var bottom = new Vector2Int(site.Anchor.X + site.Size - 1, site.Anchor.Y + site.Size - 1);
            model = new GameObject(site.Id).AddComponent<SpriteRenderer>();
            model.transform.SetParent(_parent, false);
            model.transform.position = _map.CellCentre(bottom) - new Vector3(0f, _map.Grid.cellSize.y / 2f, 0f);
            model.transform.localScale = Vector3.one * site.Size;
            model.sprite = _sites.LairOf(site.Id)?.Model;
            model.sortingOrder = MODEL_ORDER;
            _models[site.Id] = model;
            return model;
        }

        private LairBubbleView Bubble(ILairSite site)
        {
            if (_bubbles.TryGetValue(site.Id, out var bubble)) return bubble;
            bubble = UnityEngine.Object.Instantiate(_bubblePrefab, _parent);
            bubble.name = site.Id + " bubble";
            bubble.gameObject.SetActive(false);
            _bubbles[site.Id] = bubble;
            return bubble;
        }

        private GameObject Node(string name) => new(name);

        private MeshFilter Mesh(string name, int order)
        {
            var go = Node(name);
            go.transform.SetParent(_parent, false);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _art.Border;
            renderer.sortingOrder = order;
            return filter;
        }

        // How far the art's empty sky reaches down from its top, in world units: the bubble sits on the ink.
        private static float Ink(SpriteRenderer model)
        {
            var sprite = model.sprite;
            if (sprite == null || !sprite.texture.isReadable) return model.bounds.size.y * 0.1f;
            var tex = sprite.texture;
            var rect = sprite.textureRect;
            for (var y = (int)rect.yMax - 1; y >= (int)rect.yMin; y -= 4)
                for (var x = (int)rect.xMin; x < (int)rect.xMax; x += 4)
                    if (tex.GetPixel(x, y).a > 0.5f) return (rect.yMax - y) / sprite.pixelsPerUnit * model.transform.lossyScale.y;
            return 0;
        }

        private static bool Solid(SpriteRenderer model, Vector3 world)
        {
            var sprite = model.sprite;
            if (sprite == null || !sprite.texture.isReadable) return true;
            var local = model.transform.InverseTransformPoint(world);
            var px = sprite.pivot + new Vector2(local.x, local.y) * sprite.pixelsPerUnit;
            var rect = sprite.textureRect;
            if (px.x < 0 || px.y < 0 || px.x >= rect.width || px.y >= rect.height) return false;
            return sprite.texture.GetPixel((int)(rect.x + px.x), (int)(rect.y + px.y)).a > 0.3f;
        }

        private static bool Contains(Bounds bounds, Vector3 world) => world.x >= bounds.min.x && world.x <= bounds.max.x
                                                                      && world.y >= bounds.min.y && world.y <= bounds.max.y;

        // Time left, short enough for a bubble: "45s", "27m", "1h 20m", "3h", "2d 4h" — rounded up, so it never reads 0
        // while a raid is still to come.
        private string Compact(double ms)
        {
            var s = Math.Max(0, Math.Ceiling(ms / 1000));
            if (s < 60) return _numbers.Number(s) + "s";
            if (s < 3600)
            {
                var m = Math.Ceiling(s / 60);
                return m >= 60 ? "1h" : _numbers.Number(m) + "m";
            }

            var totalM = Math.Floor(s / 60);
            if (totalM < 24 * 60)
            {
                var h = Math.Floor(totalM / 60);
                var rm = totalM % 60;
                return rm > 0 ? $"{_numbers.Number(h)}h {_numbers.Number(rm)}m" : _numbers.Number(h) + "h";
            }

            var totalH = Math.Floor(totalM / 60);
            var d = Math.Floor(totalH / 24);
            var rh = totalH % 24;
            return rh > 0 ? $"{_numbers.Number(d)}d {_numbers.Number(rh)}h" : _numbers.Number(d) + "d";
        }
    }
}
