using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Battles
{
    // Cracks across a ring that has gone down: three or four grooves from the rim inward, each with a lit lower edge, so
    // the crack is cut into the material rather than drawn on it. The shape is a hash of its seed — the same every time
    // that slot falls — and it runs in from the rim as it breaks.
    public class CrackGraphic : MaskableGraphic
    {
        [SerializeField] private Color _groove = new(0.361f, 0.227f, 0.118f);
        [SerializeField] private Color _edge = new(1f, 0.945f, 0.788f, 0.75f);
        [SerializeField, Tooltip("A groove's width, as a share of the ring's diameter.")] private float _width = 0.045f;

        private readonly List<List<Vector2>> _lines = new();
        private float _progress = 1;

        public Color Edge
        {
            set
            {
                _edge = value;
                SetVerticesDirty();
            }
        }

        public void Break(int seed)
        {
            _lines.Clear();
            var x = (uint)(seed + 1) * 2654435761u;
            if (x == 0) x = 1;
            float Next()
            {
                x = x * 1664525u + 1013904223u;
                return x / 4294967296f;
            }

            var n = 3 + (Next() < 0.5f ? 1 : 0);
            var baseAngle = Next() * Mathf.PI * 2;
            for (var i = 0; i < n; i++)
            {
                var a = baseAngle + i * Mathf.PI * 2 / n + (Next() - 0.5f) * 0.6f;
                var r = 50f;
                var points = new List<Vector2> { new(Mathf.Cos(a) * r, Mathf.Sin(a) * r) };
                var stop = 8 + Next() * 14;
                for (var k = 0; k < 3; k++)
                {
                    r -= (50 - stop) / 3;
                    a += (Next() - 0.5f) * 0.7f;
                    points.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
                }

                _lines.Add(points);
            }

            Progress = 0;
        }

        // How far the cracks have run in from the rim, 0 to 1.
        public float Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            var scale = new Vector2(rect.width / 100f, rect.height / 100f);
            var width = _width * rect.width;
            foreach (var (color, shift) in new[] { (_edge, new Vector2(1.3f, -1.6f) * scale.x), (_groove, Vector2.zero) })
            {
                foreach (var line in _lines)
                {
                    // Drawn along its length as far as the progress has run.
                    var total = 0f;
                    for (var i = 1; i < line.Count; i++) total += (line[i] - line[i - 1]).magnitude;
                    var budget = total * _progress;
                    for (var i = 1; i < line.Count && budget > 0; i++)
                    {
                        var a = line[i - 1];
                        var b = line[i];
                        var seg = (b - a).magnitude;
                        if (seg > budget) b = a + (b - a) * (budget / seg);
                        budget -= seg;
                        Segment(vh, rect.center + Vector2.Scale(a, scale) + shift, rect.center + Vector2.Scale(b, scale) + shift, width, color);
                    }
                }
            }
        }

        private static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-d.y, d.x).normalized * width / 2;
            var cap = d.normalized * width / 2;
            var i = vh.currentVertCount;
            vh.AddVert(a - cap - n, color, Vector4.zero);
            vh.AddVert(a - cap + n, color, Vector4.zero);
            vh.AddVert(b + cap + n, color, Vector4.zero);
            vh.AddVert(b + cap - n, color, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
