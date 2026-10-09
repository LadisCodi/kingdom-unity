using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Battles
{
    // The playback's effects layer (Docs/features/11a-ruins-ui.md §2.7): what flies between two slots and what bursts
    // off one — arrows, bolts, slashes, thrusts, sparks, chips, helmets, dust, shocks, beams, motes — drawn as one mesh
    // over the board. Every effect is a function of the fight's clock, never of the frame: ×2 plays it twice as fast, a
    // held clock freezes it mid-air. Points are in the layer's own space (y up), sizes in design pixels (`Unit`).
    public class BattleFx : MaskableGraphic
    {
        private static readonly Color OUTLINE = Hex(0x3c2412);
        private static readonly Color SHAFT = Hex(0x7a4d26);
        private static readonly Color HEAD = Hex(0x5d6470);
        private static readonly Color FLETCH = Hex(0xf4e4c1);
        private static readonly Color CREAM = Hex(0xfff6dc);
        private static readonly Color SHADE = new(60 / 255f, 36 / 255f, 18 / 255f, 0.55f);
        private static readonly Color BOLT = Hex(0xfff3c4);
        private static readonly Color[] SPARKS = { Hex(0xfff6dc), Hex(0xffd36a), Hex(0xffb13b) };
        private static readonly Color[] WOOD = { Hex(0xa8743f), Hex(0x7a4d26), Hex(0xc99a5b) };
        private static readonly Color[] GOLD = { Hex(0xffd36a), Hex(0xe8a93a), Hex(0xfff1c9) };
        private static readonly Color[] SKY = { Hex(0xbfe3ff), Hex(0x8fc8ff), Hex(0xeef8ff) };
        public static readonly Color BOLT_GLOW = Hex(0xffc94a);

        public enum Chips
        {
            Wood,
            Gold,
            Sky,
        }

        private enum Kind
        {
            Arrow,
            Bolt,
            Slash,
            Thrust,
            Spark,
            Chip,
            Helmet,
            Dust,
            Shock,
            Beam,
            Mote,
        }

        private struct Effect
        {
            public Kind Kind;
            public Vector2 A;
            public Vector2 B;
            public Vector2 V;
            public float Start;
            public float End;
            public float Size;
            public float Angle;
            public float Spin;
            public Color Color;
        }

        private readonly List<Effect> _effects = new();
        private float _t;

        // One design pixel in this layer's units.
        public float Unit { get; set; } = 2.8f;

        public static Color Hex(int rgb, float a = 1) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);

        // Something flying from `from`, landing on `to` at `end`, bowed sideways by `bend`.
        public void Shoot(bool bolt, Vector2 from, Vector2 to, float start, float end, float bend, Color? color = null)
            => _effects.Add(new Effect { Kind = bolt ? Kind.Bolt : Kind.Arrow, A = from, B = to, Start = start, End = end, Size = bend * Unit, Color = color ?? BOLT_GLOW });

        public void Beam(Vector2 from, Vector2 to, float t, Color color)
            => _effects.Add(new Effect { Kind = Kind.Beam, A = from, B = to, Start = t, End = t + 240, Color = color });

        public void Motes(Vector2 at, float t, int n, Color color)
        {
            for (var i = 0; i < n; i++)
            {
                _effects.Add(new Effect
                {
                    Kind = Kind.Mote, A = at + new Vector2((Random.value - 0.5f) * 40 * Unit, -(Random.value - 0.2f) * 24 * Unit),
                    V = new Vector2((Random.value - 0.5f) * 0.01f * Unit, (0.03f + Random.value * 0.03f) * Unit),
                    Start = t + Random.value * 160, End = t + 600 + Random.value * 300, Size = (2.5f + Random.value * 2) * Unit, Color = color,
                });
            }
        }

        // A blade's mark on a slot along `angle` (the blow's direction, y up).
        public void Strike(bool thrust, Vector2 at, float angle, float t, float size)
            => _effects.Add(new Effect { Kind = thrust ? Kind.Thrust : Kind.Slash, A = at, Angle = angle, Start = t, End = t + (thrust ? 170 : 200), Size = size });

        public void Sparks(Vector2 at, float angle, float t, int n)
        {
            for (var i = 0; i < n; i++)
            {
                var a = angle + (Random.value - 0.5f) * 1.9f;
                var speed = (0.1f + Random.value * 0.16f) * Unit;
                _effects.Add(new Effect
                {
                    Kind = Kind.Spark, A = at, V = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed, Start = t, End = t + 220 + Random.value * 200,
                    Size = (2.4f + Random.value * 1.6f) * Unit, Color = SPARKS[i % SPARKS.Length],
                });
            }
        }

        public void Dust(Vector2 at, float t)
        {
            for (var i = 0; i < 4; i++)
            {
                var a = Mathf.PI * (0.15f + 0.7f * Random.value) + (i % 2 == 0 ? 0 : Mathf.PI);
                _effects.Add(new Effect
                {
                    Kind = Kind.Dust, A = at - new Vector2(0, 18 * Unit), V = new Vector2(Mathf.Cos(a) * 0.03f * Unit, 0.015f * Unit),
                    Start = t, End = t + 420 + Random.value * 160, Size = (5 + Random.value * 3) * Unit, Color = new Color(214 / 255f, 190 / 255f, 150 / 255f, 0.7f),
                });
            }
        }

        public void Chip(Vector2 at, float t, int n, Chips of)
        {
            var palette = of == Chips.Gold ? GOLD : of == Chips.Sky ? SKY : WOOD;
            for (var i = 0; i < n; i++)
            {
                var a = Mathf.PI / 2 + (Random.value - 0.5f) * 2.6f;
                var speed = (0.08f + Random.value * 0.12f) * Unit;
                _effects.Add(new Effect
                {
                    Kind = Kind.Chip, A = at, V = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed, Spin = (Random.value - 0.5f) * 0.03f,
                    Start = t, End = t + 520 + Random.value * 260, Size = (3 + Random.value * 3) * Unit, Color = palette[i % palette.Length],
                });
            }
        }

        public void Helmets(Vector2 at, float t, int n)
        {
            for (var i = 0; i < n; i++)
            {
                var side = (i % 2 == 0 ? 1 : -1) * (0.5f + Random.value * 0.5f);
                _effects.Add(new Effect
                {
                    Kind = Kind.Helmet, A = at + new Vector2(side * 10 * Unit, 0), V = new Vector2(side * 0.05f * Unit, 0.12f * Unit),
                    Spin = side * 0.012f, Start = t + i * 60, End = t + i * 60 + 620, Size = 7 * Unit,
                });
            }
        }

        public void Shock(Vector2 at, float t, float size, Color? color = null)
            => _effects.Add(new Effect { Kind = Kind.Shock, A = at, Start = t, End = t + 380, Size = size * Unit, Color = color ?? CREAM });

        // Draws the clock's `t`, dropping whatever has finished.
        public void Draw(float t)
        {
            _t = t;
            _effects.RemoveAll(e => t >= e.End);
            SetVerticesDirty();
        }

        public void Clear()
        {
            _effects.Clear();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach (var e in _effects)
            {
                if (_t < e.Start) continue;
                var p = (_t - e.Start) / Mathf.Max(1, e.End - e.Start);
                var age = _t - e.Start;
                var u = Unit;
                switch (e.Kind)
                {
                    case Kind.Arrow:
                    {
                        var (pos, a) = Along(e, p);
                        DrawArrow(vh, pos, a);
                        break;
                    }
                    case Kind.Bolt:
                        for (var i = 3; i >= 0; i--)
                        {
                            var (bead, _) = Along(e, Mathf.Max(0, p - i * 0.06f));
                            var c = i == 0 ? BOLT : e.Color;
                            c.a = i == 0 ? 1 : 0.5f - i * 0.12f;
                            Disc(vh, bead, (i == 0 ? 5.5f : 4.5f - i) * u, c);
                        }

                        Disc(vh, Along(e, p).Item1, 10 * u, Fade(e.Color, 0.35f));
                        break;
                    case Kind.Slash:
                    {
                        var r = 30 * u * e.Size;
                        const float sweep = 2.4f;
                        var a0 = e.Angle - Mathf.PI / 2 - sweep / 2;
                        var head = a0 + sweep * Ease(Mathf.Min(1, p * 1.6f));
                        var tail = a0 + sweep * Ease(Mathf.Max(0, p * 1.6f - 0.6f));
                        var centre = e.A - new Vector2(Mathf.Cos(e.Angle), Mathf.Sin(e.Angle)) * r * 0.35f;
                        var alpha = 1 - p * p;
                        Arc(vh, centre, r, tail, head, 11 * u * e.Size * (1 - p * 0.5f), Fade(SHADE, SHADE.a * alpha));
                        Arc(vh, centre, r, tail, head, 6 * u * e.Size * (1 - p * 0.5f), Fade(CREAM, alpha));
                        break;
                    }
                    case Kind.Thrust:
                    {
                        var reach = 30 * u * e.Size;
                        var head = Ease(Mathf.Min(1, p * 1.8f));
                        var tail = Ease(Mathf.Max(0, p * 1.8f - 0.7f));
                        var dir = new Vector2(Mathf.Cos(e.Angle), Mathf.Sin(e.Angle));
                        var origin = e.A - dir * reach;
                        var alpha = 1 - p * p;
                        Line(vh, origin + dir * reach * 1.5f * tail, origin + dir * reach * 1.5f * head, 10 * u * e.Size, Fade(SHADE, SHADE.a * alpha));
                        Line(vh, origin + dir * reach * 1.5f * tail, origin + dir * reach * 1.5f * head, 5 * u * e.Size, Fade(CREAM, alpha));
                        break;
                    }
                    case Kind.Spark:
                    {
                        // Falls under gravity: y up, so the pull is down.
                        var pos = e.A + e.V * age - new Vector2(0, 0.0004f * u * age * age);
                        var s = e.Size * (1 - p * 0.6f);
                        var a = Mathf.Atan2(e.V.y - 0.0008f * u * age, e.V.x);
                        var alpha = Mathf.Min(1, 2 * (1 - p));
                        var diamond = new[] { new Vector2(s * 2.2f, 0), new Vector2(0, -s * 0.8f), new Vector2(-s * 1.4f, 0), new Vector2(0, s * 0.8f) };
                        Polygon(vh, pos, a, diamond, Fade(e.Color, alpha), Fade(OUTLINE, alpha), 1.4f * u);
                        break;
                    }
                    case Kind.Chip:
                    {
                        var pos = e.A + e.V * age - new Vector2(0, 0.0005f * u * age * age);
                        var s = e.Size;
                        var alpha = Mathf.Min(1, 3 * (1 - p));
                        var shard = new[] { new Vector2(-s, s * 0.35f), new Vector2(s, s * 0.15f), new Vector2(s * 0.7f, -s * 0.35f), new Vector2(-s * 0.8f, -s * 0.25f) };
                        Polygon(vh, pos, e.Spin * age, shard, Fade(e.Color, alpha), Fade(OUTLINE, alpha), 1.2f * u);
                        break;
                    }
                    case Kind.Helmet:
                    {
                        var pos = e.A + e.V * age - new Vector2(0, 0.0006f * u * age * age);
                        var s = e.Size;
                        var alpha = Mathf.Min(1, 4 * (1 - p));
                        var rot = e.Spin * age;
                        var dome = new Vector2[9];
                        for (var i = 0; i <= 8; i++) dome[i] = new Vector2(Mathf.Cos(Mathf.PI * i / 8) * s, Mathf.Sin(Mathf.PI * i / 8) * s);
                        Polygon(vh, pos, rot, dome, Fade(Hex(0xaab3bd), alpha), Fade(OUTLINE, alpha), 1.3f * u);
                        Polygon(vh, pos, rot, new[] { new Vector2(-s * 1.25f, 0), new Vector2(s * 1.25f, 0), new Vector2(s * 1.25f, -s * 0.45f), new Vector2(-s * 1.25f, -s * 0.45f) },
                            Fade(Hex(0x7d8793), alpha), Fade(OUTLINE, alpha), 1.3f * u);
                        Polygon(vh, pos, rot, new[] { new Vector2(-s * 0.15f, 0), new Vector2(s * 0.15f, 0), new Vector2(s * 0.15f, -s * 0.9f), new Vector2(-s * 0.15f, -s * 0.9f) },
                            Fade(Hex(0x7d8793), alpha), Fade(OUTLINE, alpha), 1.3f * u);
                        Disc(vh, pos + Rotate(new Vector2(-s * 0.35f, s * 0.45f), rot), s * 0.22f, Fade(Hex(0xe4e9ee), alpha));
                        break;
                    }
                    case Kind.Shock:
                        Ring(vh, e.A, e.Size * (0.6f + Ease(p) * 0.9f), 4 * u * (1 - p), Fade(e.Color, 0.8f * (1 - p)));
                        break;
                    case Kind.Beam:
                    {
                        var from = e.A + (e.B - e.A) * Ease(p);
                        Line(vh, from, e.B, 9 * u * (1 - p), Fade(e.Color, 1 - p));
                        Line(vh, from, e.B, 3 * u * (1 - p), Fade(Color.white, 1 - p));
                        break;
                    }
                    case Kind.Mote:
                    {
                        var pos = e.A + e.V * age + new Vector2(Mathf.Sin(age / 90) * 2 * u, 0);
                        var alpha = Mathf.Min(1, 4 * p) * (1 - p);
                        Disc(vh, pos, e.Size * 2.2f, Fade(e.Color, alpha * 0.5f));
                        Disc(vh, pos, e.Size * 0.8f, Fade(Hex(0xfffbe8), alpha));
                        break;
                    }
                    case Kind.Dust:
                        Disc(vh, e.A + e.V * age, e.Size * (0.6f + p), Fade(e.Color, 0.7f * (1 - p)));
                        break;
                }
            }
        }

        private static float Ease(float p) => 1 - (1 - p) * (1 - p);

        private static Color Fade(Color c, float a)
        {
            c.a = Mathf.Clamp01(a);
            return c;
        }

        private static Vector2 Rotate(Vector2 v, float a)
        {
            var c = Mathf.Cos(a);
            var s = Mathf.Sin(a);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        // Where a projectile is at `p` of its flight, bowed sideways (a lob seen from above), and which way it points.
        private static (Vector2, float) Along(Effect e, float p)
        {
            var d = e.B - e.A;
            var len = Mathf.Max(0.001f, d.magnitude);
            var n = new Vector2(-d.y / len, d.x / len);
            var lift = e.Size * 4 * p * (1 - p);
            var dlift = e.Size * 4 * (1 - 2 * p);
            return (e.A + d * p + n * lift, Mathf.Atan2(d.y + n.y * dlift, d.x + n.x * dlift));
        }

        private void DrawArrow(VertexHelper vh, Vector2 at, float a)
        {
            var u = Unit;
            var len = 16 * u;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Line(vh, at - dir * len, at, 3.4f * u, OUTLINE);
            Line(vh, at - dir * len, at, 1.8f * u, SHAFT);
            Polygon(vh, at, a, new[] { new Vector2(4 * u, 0), new Vector2(-2 * u, 3 * u), new Vector2(-2 * u, -3 * u) }, HEAD, OUTLINE, 1.2f * u);
            Polygon(vh, at, a, new[] { new Vector2(-len + 5 * u, 0), new Vector2(-len - u, 3.5f * u), new Vector2(-len + u, 0), new Vector2(-len - u, -3.5f * u) },
                FLETCH, OUTLINE, 1.2f * u);
        }

        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            if (width <= 0) return;
            var d = b - a;
            if (d.sqrMagnitude < 1e-6f) d = Vector2.right * 0.001f;
            var n = new Vector2(-d.y, d.x).normalized * width / 2;
            var cap = d.normalized * width / 2;
            Quad(vh, a - cap - n, a - cap + n, b + cap + n, b + cap - n, color);
        }

        private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            var i = vh.currentVertCount;
            vh.AddVert(a, color, Vector4.zero);
            vh.AddVert(b, color, Vector4.zero);
            vh.AddVert(c, color, Vector4.zero);
            vh.AddVert(d, color, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        private static void Disc(VertexHelper vh, Vector2 at, float r, Color color)
        {
            if (r <= 0) return;
            const int SEGMENTS = 16;
            var centre = vh.currentVertCount;
            vh.AddVert(at, color, Vector4.zero);
            for (var i = 0; i <= SEGMENTS; i++)
            {
                var a = Mathf.PI * 2 * i / SEGMENTS;
                vh.AddVert(at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, color, Vector4.zero);
                if (i > 0) vh.AddTriangle(centre, centre + i, centre + i + 1);
            }
        }

        private static void Ring(VertexHelper vh, Vector2 at, float r, float width, Color color) => Arc(vh, at, r, 0, Mathf.PI * 2, width, color);

        private static void Arc(VertexHelper vh, Vector2 at, float r, float from, float to, float width, Color color)
        {
            if (width <= 0 || to <= from) return;
            var segments = Mathf.Max(4, Mathf.CeilToInt((to - from) / 0.15f));
            var start = vh.currentVertCount;
            for (var i = 0; i <= segments; i++)
            {
                var a = Mathf.Lerp(from, to, i / (float)segments);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                vh.AddVert(at + dir * (r - width / 2), color, Vector4.zero);
                vh.AddVert(at + dir * (r + width / 2), color, Vector4.zero);
                if (i == 0) continue;
                var k = start + i * 2;
                vh.AddTriangle(k - 2, k - 1, k + 1);
                vh.AddTriangle(k - 2, k + 1, k);
            }
        }

        // A convex shape about `at`, turned by `angle`, filled and outlined.
        private static void Polygon(VertexHelper vh, Vector2 at, float angle, Vector2[] points, Color fill, Color outline, float stroke)
        {
            var world = new Vector2[points.Length];
            for (var i = 0; i < points.Length; i++) world[i] = at + Rotate(points[i], angle);
            for (var i = 0; i < world.Length; i++) Line(vh, world[i], world[(i + 1) % world.Length], stroke, outline);
            var start = vh.currentVertCount;
            foreach (var p in world) vh.AddVert(p, fill, Vector4.zero);
            for (var i = 1; i < world.Length - 1; i++) vh.AddTriangle(start, start + i, start + i + 1);
        }
    }
}
