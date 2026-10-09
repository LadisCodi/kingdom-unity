using System;
using System.Collections.Generic;
using Codigames.Game.Map;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Map;
using Codigames.Modules.Grid;
using UnityEngine;
using VContainer.Unity;
using Vector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Fog
{
    // The Townhall's reach, drawn as the player's border: a line in their blue with a soft glow along the last ring
    // that may be paid for, over the fog and across undiscovered ground, so the border is read off the map before a
    // tap is refused. Gone once the reach holds the whole province.
    public class ReachBorderView : IStartable, IDisposable
    {
        private const int SORTING_ORDER = 550;
        private const float LINE = 0.05f;
        private const float BLUR = 0.16f;
        private static readonly Color BLUE = new Color32(0x2f, 0x6f, 0xe0, 0xff);

        // The glow, wide and faint to narrow and solid.
        private static readonly (float Width, float Alpha)[] LAYERS =
        {
            (LINE + BLUR * 1.1f, 0.08f), (LINE + BLUR * 0.7f, 0.12f), (LINE + BLUR * 0.35f, 0.2f), (LINE, 1f),
        };

        private readonly FogOfWar _fog;
        private readonly IProvinceMap _province;
        private readonly ProvinceMap _map;
        private readonly Construction _construction;
        private readonly SightArt _art;

        private MeshFilter _filter;
        private int _drawnReach = -1;

        public ReachBorderView(FogOfWar fog, IProvinceMap province, ProvinceMap map, Construction construction, SightArt art)
        {
            _art = art;
            _fog = fog;
            _province = province;
            _map = map;
            _construction = construction;
        }

        public void Start()
        {
            var go = new GameObject("ReachBorder");
            _filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _art.Border;
            renderer.sortingOrder = SORTING_ORDER;
            Refresh();
            _construction.JobCompleted += OnJobCompleted;
        }

        public void Dispose() => _construction.JobCompleted -= OnJobCompleted;

        private void OnJobCompleted(ConstructionJob job, DistrictState district) => Refresh();

        private void Refresh()
        {
            if (_fog.Reach == _drawnReach) return;

            _drawnReach = _fog.Reach;
            var edges = new List<(Vector3 A, Vector3 B)>();
            foreach (var cell in _province.Cells)
            {
                if (!_fog.IsWithinReach(cell)) continue;

                foreach (var neighbour in GridMath.Neighbours(cell))
                {
                    if (_province.Contains(neighbour) && !_fog.IsWithinReach(neighbour)) edges.Add(SharedEdge(cell, neighbour));
                }
            }

            _filter.sharedMesh = Build(edges);
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

        private static Mesh Build(IReadOnlyList<(Vector3 A, Vector3 B)> edges)
        {
            var vertices = new List<Vector3>();
            var colours = new List<Color>();
            var triangles = new List<int>();
            foreach (var (width, alpha) in LAYERS)
            {
                var colour = new Color(BLUE.r, BLUE.g, BLUE.b, alpha);
                foreach (var (a, b) in edges)
                {
                    var along = (b - a).normalized;
                    var across = new Vector3(-along.y, along.x, 0f) * (width / 2f);
                    var cap = along * (width / 2f);
                    var start = vertices.Count;
                    vertices.Add(a - cap - across);
                    vertices.Add(a - cap + across);
                    vertices.Add(b + cap + across);
                    vertices.Add(b + cap - across);
                    for (var i = 0; i < 4; i++) colours.Add(colour);
                    triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                }
            }

            var mesh = new Mesh { name = "Reach" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetColors(colours);
            mesh.SetTriangles(triangles, 0);
            return mesh;
        }
    }
}
