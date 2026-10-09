using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Fog;
using Codigames.Game.Map;
using Codigames.Modules.Grid;
using UnityEngine;
using VContainer.Unity;
using Vector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.City
{
    // A building's work area (the web's areaOverlays.drawArea): a sky-blue glow inside its edge, breathing, under a
    // white line — on the floor, under everything that stands — and the features its crew would work rimmed in
    // white. Drawn while a producer is placed or moved, and while its card is open.
    public class WorkAreaView : ITickable, IDisposable
    {
        private const int GLOW_ORDER = -47;
        private const int LINE_ORDER = -46;
        // The rims behind the features they ring.
        private const int RIM_ORDER = -1;
        private const float LINE = 0.05f;
        private const float GLOW_ALPHA = 0.7f;
        private const float GLOW_DEPTH = 0.9f;
        private const float BREATH_SECONDS = 2.6f;
        private const float BREATH_LOW = 0.45f;
        private const float RIM = 0.04f;
        private static readonly Color GLOW = new Color32(125, 205, 255, 255);
        private static readonly Color LINE_COLOUR = new(1f, 1f, 1f, 0.9f);
        private static readonly Color RIM_COLOUR = new Color32(0xff, 0xf8, 0xe1, 0xff);
        private static readonly Vector2[] RIM_DIRECTIONS =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1), new(0.71f, 0.71f), new(-0.71f, 0.71f), new(0.71f, -0.71f), new(-0.71f, -0.71f),
        };

        private readonly ProvinceMap _map;
        private readonly SightArt _art;
        private readonly List<SpriteRenderer> _rims = new();

        private GameObject _root;
        private MeshFilter _glow;
        private MeshFilter _line;
        private Material _rimMaterial;
        private Color[] _glowColours;
        private Color[] _breathing;
        private string _drawn;

        public WorkAreaView(ProvinceMap map, SightArt art)
        {
            _map = map;
            _art = art;
        }

        // The area and the cells its crew would work; drawn again only when either changes.
        public void Show(IReadOnlyCollection<Vector2Int> area, IReadOnlyCollection<Vector2Int> worked)
        {
            var key = string.Join(";", area.Select(c => c.X + "," + c.Y)) + "|" + string.Join(";", worked.Select(c => c.X + "," + c.Y));
            if (key == _drawn && _root != null && _root.activeSelf) return;
            _drawn = key;
            Make();
            _root.SetActive(true);

            var set = new HashSet<Vector2Int>(area);
            var edges = new List<(Vector3 A, Vector3 B, Vector3 Across)>();
            foreach (var cell in set)
            {
                foreach (var neighbour in GridMath.Neighbours(cell))
                {
                    if (set.Contains(neighbour)) continue;
                    var (a, b) = SharedEdge(cell, neighbour);
                    // Across the cell, from this edge to the one facing it: the way the glow fades in.
                    var across = 2f * (_map.CellCentre(cell) - (a + b) / 2f);
                    edges.Add((a, b, across));
                }
            }

            _glow.sharedMesh = Glow(edges, out _glowColours);
            _breathing = new Color[_glowColours.Length];
            var line = new Mesh { name = "Work area line" };
            GlowLines.Build(line, edges.Select(e => (e.A, e.B)).ToList(), new[] { (_map.Grid.cellSize.x * LINE, LINE_COLOUR) });
            _line.sharedMesh = line;
            Rims(worked);
        }

        public void Hide()
        {
            _drawn = null;
            if (_root != null) _root.SetActive(false);
        }

        public void Tick()
        {
            if (_root == null || !_root.activeSelf || _glowColours == null) return;
            var phase = Time.unscaledTime % BREATH_SECONDS / BREATH_SECONDS;
            var breath = BREATH_LOW + (1 - BREATH_LOW) * (0.5f - 0.5f * Mathf.Cos(2 * Mathf.PI * phase));
            for (var i = 0; i < _glowColours.Length; i++)
            {
                var c = _glowColours[i];
                _breathing[i] = new Color(c.r, c.g, c.b, c.a * breath);
            }

            _glow.sharedMesh.colors = _breathing;
        }

        public void Dispose()
        {
            if (_rimMaterial != null) UnityEngine.Object.Destroy(_rimMaterial);
            if (_root != null) UnityEngine.Object.Destroy(_root);
        }

        private void Make()
        {
            if (_root != null) return;
            _root = new GameObject("WorkArea");
            _glow = Layer("Glow", GLOW_ORDER);
            _line = Layer("Line", LINE_ORDER);
            _rimMaterial = new Material(_art.Silhouette);
            _rimMaterial.SetColor("_Fill", RIM_COLOUR);
        }

        private MeshFilter Layer(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _art.Border;
            renderer.sortingOrder = order;
            return filter;
        }

        // A band inside each edge: the glow's colour at the edge, half of it midway, none `GLOW_DEPTH` of a cell in.
        private static Mesh Glow(IReadOnlyList<(Vector3 A, Vector3 B, Vector3 Across)> edges, out Color[] colours)
        {
            var vertices = new List<Vector3>();
            var tints = new List<Color>();
            var triangles = new List<int>();
            var stops = new[] { (0f, GLOW_ALPHA), (0.5f, GLOW_ALPHA * 0.45f), (1f, 0f) };
            foreach (var (a, b, across) in edges)
            {
                var v = across * GLOW_DEPTH;
                var start = vertices.Count;
                foreach (var (at, alpha) in stops)
                {
                    vertices.Add(a + v * at);
                    vertices.Add(b + v * at);
                    tints.Add(new Color(GLOW.r, GLOW.g, GLOW.b, alpha));
                    tints.Add(new Color(GLOW.r, GLOW.g, GLOW.b, alpha));
                }

                for (var row = 0; row < 2; row++)
                {
                    var r = start + row * 2;
                    triangles.AddRange(new[] { r, r + 1, r + 3, r, r + 3, r + 2 });
                }
            }

            var mesh = new Mesh { name = "Work area glow" };
            mesh.SetVertices(vertices);
            mesh.SetColors(tints);
            mesh.SetTriangles(triangles, 0);
            colours = tints.ToArray();
            return mesh;
        }

        // Each worked feature's drawing, filled white and nudged round it: its rim, behind it.
        private void Rims(IReadOnlyCollection<Vector2Int> worked)
        {
            var features = _map.Features;
            var rim = _map.Grid.cellSize.x * RIM;
            var needed = 0;
            foreach (var cell in worked)
            {
                var position = ProvinceCoordinates.ToTilemap(cell);
                var sprite = features.GetSprite(position);
                if (sprite == null) continue;
                var at = features.GetCellCenterWorld(position);
                var matrix = features.GetTransformMatrix(position);
                foreach (var direction in RIM_DIRECTIONS)
                {
                    if (needed == _rims.Count) _rims.Add(Rim());
                    var renderer = _rims[needed++];
                    renderer.gameObject.SetActive(true);
                    renderer.sprite = sprite;
                    renderer.transform.position = at + matrix.GetPosition() + (Vector3)(direction * rim);
                    renderer.transform.localScale = matrix.lossyScale;
                }
            }

            for (var i = needed; i < _rims.Count; i++) _rims[i].gameObject.SetActive(false);
        }

        private SpriteRenderer Rim()
        {
            var go = new GameObject("Rim");
            go.transform.SetParent(_root.transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = _rimMaterial;
            renderer.sortingOrder = RIM_ORDER;
            return renderer;
        }

        // The side of a cell's diamond that faces a neighbour.
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
    }
}
