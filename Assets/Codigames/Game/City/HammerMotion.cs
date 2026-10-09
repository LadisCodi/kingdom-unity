using UnityEngine;

namespace Codigames.Game.City
{
    // The builder's hammer at work, as the web's district.css draws it: a 3.2-second loop of a wind-up, a blow that
    // sparks, a flurry of taps and two more blows, in an 84 × 75 frame (y down). Pure: the map's hammer and the card
    // portrait's both read it.
    public static class HammerMotion
    {
        public const float LOOP_SECONDS = 3.2f;
        public const float FRAME_WIDTH = 84f;
        public const float HAMMER_SIZE = 40f;

        // At t (0…1 of the loop), the value, and the easing of the stretch to the next (district.css).
        private static readonly (float T, float V, int Ease)[] ROTATE =
        {
            (0, -20, IN_OUT), (0.09f, -24, BLOW), (0.15f, 70, OUT), (0.2f, 58, IN_OUT), (0.3f, -20, LINEAR), (0.32f, -26, LINEAR),
            (0.345f, -34, LINEAR), (0.37f, -22, LINEAR), (0.395f, -8, LINEAR), (0.42f, -14, LINEAR), (0.445f, -24, LINEAR),
            (0.47f, -16, OUT), (0.5f, -20, IN_OUT), (0.54f, 30, IN), (0.57f, 70, OUT), (0.63f, 30, IN), (0.66f, 70, OUT),
            (0.74f, -20, IN_OUT), (0.83f, -14, IN_OUT), (0.92f, -20, IN_OUT), (1, -20, IN_OUT),
        };

        private static readonly (float T, Vector2 V, int Ease)[] TRANSLATE =
        {
            (0, new(0, 0), IN_OUT), (0.3f, new(0, 0), LINEAR), (0.32f, new(9, 4), LINEAR), (0.345f, new(11, 14), LINEAR),
            (0.37f, new(3, 20), LINEAR), (0.395f, new(-5, 13), LINEAR), (0.42f, new(-2, 3), LINEAR), (0.445f, new(-14, 7), LINEAR),
            (0.47f, new(-27, 1), OUT), (0.5f, new(-40, 4), IN_OUT), (0.74f, new(-40, 4), IN_OUT), (0.83f, new(-20, 10), IN_OUT),
            (0.92f, new(0, 0), IN_OUT), (1, new(0, 0), IN_OUT),
        };

        // When each blow lands in the loop, where, and how far its four specks fly.
        public static readonly (float At, float Life, float X, float Y, float Reach)[] BURSTS =
        {
            (0.15f, 0.11f, 70, 20, 16), (0.57f, 0.05f, 30, 24, 10), (0.66f, 0.06f, 30, 24, 10),
        };

        public static readonly Vector2[] SPECKS = { new(1, -1.1f), new(1.4f, -0.3f), new(-0.6f, -1.3f), new(0.4f, -1.5f) };

        private const int LINEAR = 0;
        private const int IN_OUT = 1;
        private const int IN = 2;
        private const int OUT = 3;
        private const int BLOW = 4;

        // Where in its loop a site's hammer is at a moment.
        public static float At(float time, float phase) => Mathf.Repeat(time / LOOP_SECONDS + phase, 1f);

        // The handle's turn (degrees, clockwise as the web's) and the frame's shift, at k of the loop.
        public static float Rotation(float k) => Sample(ROTATE, k);
        public static Vector2 Shift(float k) => SampleVector(TRANSLATE, k);

        private static float Sample((float T, float V, int Ease)[] track, float k)
        {
            var i = Segment(k, track.Length, n => track[n].T);
            var e = Ease(track[i].Ease, Progress(k, track[i].T, track[i + 1].T));
            return Mathf.Lerp(track[i].V, track[i + 1].V, e);
        }

        private static Vector2 SampleVector((float T, Vector2 V, int Ease)[] track, float k)
        {
            var i = Segment(k, track.Length, n => track[n].T);
            var e = Ease(track[i].Ease, Progress(k, track[i].T, track[i + 1].T));
            return Vector2.Lerp(track[i].V, track[i + 1].V, e);
        }

        private static int Segment(float k, int length, System.Func<int, float> at)
        {
            var i = 0;
            while (i < length - 2 && at(i + 1) <= k) i++;
            return i;
        }

        private static float Progress(float k, float from, float to) => to <= from ? 1f : Mathf.Clamp01((k - from) / (to - from));

        private static float Ease(int ease, float x) => ease switch
        {
            IN_OUT => Bezier(0.42f, 0, 0.58f, 1, x),
            IN => Bezier(0.42f, 0, 1, 1, x),
            OUT => Bezier(0, 0, 0.58f, 1, x),
            BLOW => Bezier(0.6f, 0, 1, 0.6f, x),
            _ => x,
        };

        // CSS's cubic-bezier: the curve's y where its x is `x`.
        private static float Bezier(float x1, float y1, float x2, float y2, float x)
        {
            var t = x;
            for (var i = 0; i < 8; i++)
            {
                var dx = Cubic(x1, x2, t) - x;
                var slope = 3 * (1 - t) * (1 - t) * x1 + 6 * (1 - t) * t * (x2 - x1) + 3 * t * t * (1 - x2);
                if (Mathf.Abs(slope) < 1e-5f) break;
                t = Mathf.Clamp01(t - dx / slope);
            }

            return Cubic(y1, y2, t);
        }

        private static float Cubic(float p1, float p2, float t) => 3 * (1 - t) * (1 - t) * t * p1 + 3 * (1 - t) * t * t * p2 + t * t * t;

        // A stable phase per site, so two do not hammer in step.
        public static float Phase(string id)
        {
            unchecked
            {
                var h = 2166136261u;
                foreach (var c in id ?? "") h = (h ^ c) * 16777619u;
                return h % 3200 / 3200f;
            }
        }
    }
}
