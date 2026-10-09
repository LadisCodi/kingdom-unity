using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Effects
{
    public enum ParticleKind
    {
        // A four-pointed glint that flares and fades.
        Spark,
        // A paper flake that tumbles and falls.
        Confetti,
        // A soft puff that swells and drifts.
        Dust,
        // An ember that rises slowly, glowing.
        Mote,
    }

    // One burst: how many, of what, how fast and which way, how big and for how long — in the web's px (Unit scales
    // them), randomised as the web's particle layer does.
    public struct Burst
    {
        public ParticleKind Kind;
        public int Count;
        public Color[] Colors;
        public float Speed;
        // Radians, y down as on the web; null all around.
        public float? Angle;
        public float Spread;
        public float Size;
        public float Life;
        public float Gravity;
        public float Radius;
    }

    // A particle layer for the full-screen moments (the web's particles.ts): one graphic over the screen it decorates,
    // drawing while something is alive. Presentation only, so Unity's Random is fine. Positions are in this graphic's
    // local space with y down, so the web's numbers read as they are.
    [RequireComponent(typeof(CanvasRenderer))]
    public class ParticleLayer : MaskableGraphic
    {
        private const int ROUND = 14;
        private static readonly Color[] EMBERS = { new(1, 0.77f, 0.36f, 0.9f), new(1, 0.89f, 0.63f, 0.9f) };

        private struct Particle
        {
            public ParticleKind Kind;
            public Vector2 P;
            public Vector2 V;
            public float Size;
            public Color Color;
            public float Age;
            public float Life;
            public float Gravity;
            public float Rot;
            public float Spin;
        }

        private readonly List<Particle> _parts = new();
        private bool _ambient;
        private float _debt;

        // Rpx a web px.
        public float Unit { get; set; } = 2.8f;

        public bool Alive => _parts.Count > 0 || _ambient;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void Emit(Vector2 at, Burst o)
        {
            for (var i = 0; i < o.Count; i++)
            {
                var a = o.Angle.HasValue ? o.Angle.Value + (Random.value - 0.5f) * (o.Spread > 0 ? o.Spread : Mathf.PI / 3) : Random.value * Mathf.PI * 2;
                var v = Rnd(o.Speed > 0 ? o.Speed : 220, 0.5f) * Unit;
                var r = o.Radius * Unit * Mathf.Sqrt(Random.value);
                var ra = Random.value * Mathf.PI * 2;
                _parts.Add(new Particle
                {
                    Kind = o.Kind,
                    P = at + new Vector2(Mathf.Cos(ra), Mathf.Sin(ra)) * r,
                    V = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v,
                    Size = Rnd(o.Size > 0 ? o.Size : 6, 0.4f) * Unit,
                    Color = o.Colors[Random.Range(0, o.Colors.Length)],
                    Life = Rnd(o.Life > 0 ? o.Life : 900, 0.3f),
                    Gravity = o.Gravity * Unit,
                    Rot = Random.value * Mathf.PI * 2,
                    Spin = (Random.value - 0.5f) * 12,
                });
            }
        }

        // A slow drift of embers up the screen while on.
        public void Ambient(bool on) => _ambient = on;

        public void Clear()
        {
            _parts.Clear();
            _ambient = false;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (!Alive && _parts.Count == 0) return;
            var dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            var size = rectTransform.rect.size;
            if (_ambient)
            {
                _debt += dt * 5;
                while (_debt >= 1)
                {
                    _debt -= 1;
                    Emit(new Vector2(Random.value * size.x, size.y * (0.75f + Random.value * 0.3f)), new Burst
                    {
                        Kind = ParticleKind.Mote, Count = 1, Colors = EMBERS, Speed = 30, Angle = -Mathf.PI / 2, Spread = 0.8f, Size = 2.2f, Life = 4200,
                    });
                }
            }

            for (var i = _parts.Count - 1; i >= 0; i--)
            {
                var p = _parts[i];
                p.Age += dt * 1000;
                if (p.Age >= p.Life)
                {
                    _parts.RemoveAt(i);
                    continue;
                }

                p.V.y += p.Gravity * dt;
                if (p.Kind == ParticleKind.Confetti)
                {
                    p.V.x *= 1 - 1.6f * dt;
                    p.V.y = Mathf.Min(p.V.y, 140 * Unit);
                }

                if (p.Kind == ParticleKind.Dust) p.V *= 1 - 3 * dt;
                p.P += p.V * dt;
                p.Rot += p.Spin * dt;
                _parts[i] = p;
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            // The web's y runs down from the top-left corner.
            Vector2 Local(Vector2 p) => new(rect.xMin + p.x, rect.yMax - p.y);
            foreach (var p in _parts)
            {
                var t = p.Age / p.Life;
                var at = Local(p.P);
                switch (p.Kind)
                {
                    case ParticleKind.Spark:
                    {
                        var s = p.Size * (t < 0.2f ? t / 0.2f : 1 - (t - 0.2f) / 0.8f);
                        Star(vh, at, s, p.Rot, Alpha(p.Color, 1 - t * 0.6f));
                        break;
                    }
                    case ParticleKind.Confetti:
                    {
                        var c = Alpha(p.Color, t > 0.75f ? (1 - t) / 0.25f : 1);
                        var w = p.Size * Mathf.Cos(p.Age / 90 + p.Rot);
                        Quad(vh, at, w, p.Size * 0.6f, p.Rot, c);
                        break;
                    }
                    case ParticleKind.Dust:
                        Disc(vh, at, p.Size * (0.6f + t * 1.2f), Alpha(p.Color, 0.55f * (1 - t)), Alpha(p.Color, 0.55f * (1 - t)));
                        break;
                    default:
                    {
                        var a = Mathf.Sin(Mathf.PI * t) * 0.8f;
                        Disc(vh, at, p.Size * 2, Alpha(p.Color, a), new Color(1, 0.7f, 0.24f, 0));
                        break;
                    }
                }
            }
        }

        private static Color Alpha(Color c, float a) => new(c.r, c.g, c.b, c.a * Mathf.Clamp01(a));

        // A four-pointed glint: its points out on the axes, pinched to the middle between them.
        private static void Star(VertexHelper vh, Vector2 at, float s, float rot, Color c)
        {
            var start = vh.currentVertCount;
            var linear = c.linear;
            vh.AddVert(at, linear, Vector4.zero);
            for (var i = 0; i < 8; i++)
            {
                var a = -rot + i * Mathf.PI / 4;
                var r = i % 2 == 0 ? s : s * 0.18f;
                vh.AddVert(at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, linear, Vector4.zero);
            }

            for (var i = 0; i < 8; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 8);
        }

        private static void Quad(VertexHelper vh, Vector2 at, float w, float h, float rot, Color c)
        {
            var start = vh.currentVertCount;
            var linear = c.linear;
            var cos = Mathf.Cos(-rot);
            var sin = Mathf.Sin(-rot);
            Vector2 R(float x, float y) => at + new Vector2(x * cos - y * sin, x * sin + y * cos);
            vh.AddVert(R(-w / 2, -h / 2), linear, Vector4.zero);
            vh.AddVert(R(-w / 2, h / 2), linear, Vector4.zero);
            vh.AddVert(R(w / 2, h / 2), linear, Vector4.zero);
            vh.AddVert(R(w / 2, -h / 2), linear, Vector4.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void Disc(VertexHelper vh, Vector2 at, float r, Color centre, Color edge)
        {
            var start = vh.currentVertCount;
            vh.AddVert(at, centre.linear, Vector4.zero);
            var e = edge.linear;
            for (var i = 0; i < ROUND; i++)
            {
                var a = i * Mathf.PI * 2 / ROUND;
                vh.AddVert(at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, e, Vector4.zero);
            }

            for (var i = 0; i < ROUND; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % ROUND);
        }

        private static float Rnd(float b, float spread) => b * (1 - spread + Random.value * spread * 2);
    }
}
