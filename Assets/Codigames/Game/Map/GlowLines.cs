using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.Map
{
    // Lines drawn on the map as flat quads in layers, wide and faint under narrow and bright: a glow without a blur.
    public static class GlowLines
    {
        public static void Build(Mesh mesh, IReadOnlyList<(Vector3 A, Vector3 B)> edges, IReadOnlyList<(float Width, Color Colour)> layers)
        {
            var vertices = new List<Vector3>();
            var colours = new List<Color>();
            var triangles = new List<int>();
            foreach (var (width, colour) in layers)
            {
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

            mesh.Clear();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetColors(colours);
            mesh.SetTriangles(triangles, 0);
        }
    }
}
