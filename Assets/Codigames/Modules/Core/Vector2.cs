using System;
using System.Globalization;

namespace Codigames.Modules.Core
{
    // A point or a direction in 2D, ours rather than an engine's or a library's, so it serialises exactly as
    // written: two public fields, X and Y. Game converts it to and from Unity's at the edge.
    [Serializable]
    public struct Vector2 : IEquatable<Vector2>
    {
        // Public fields, so every serialiser (Unity's included) writes the same two numbers.
        public float X;
        public float Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 Zero => new(0f, 0f);
        public static Vector2 One => new(1f, 1f);

        public float SqrMagnitude => X * X + Y * Y;
        public float Magnitude => MathF.Sqrt(SqrMagnitude);

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator -(Vector2 a) => new(-a.X, -a.Y);
        public static Vector2 operator *(Vector2 a, float k) => new(a.X * k, a.Y * k);
        public static Vector2 operator *(float k, Vector2 a) => new(a.X * k, a.Y * k);
        public static Vector2 operator /(Vector2 a, float k) => new(a.X / k, a.Y / k);
        public static bool operator ==(Vector2 a, Vector2 b) => a.Equals(b);
        public static bool operator !=(Vector2 a, Vector2 b) => !a.Equals(b);

        // Linear interpolation, t clamped to [0, 1].
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new Vector2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        public static float Distance(Vector2 a, Vector2 b) => (a - b).Magnitude;

        public bool Equals(Vector2 other) => X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object obj) => obj is Vector2 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString()
            => "(" + X.ToString(CultureInfo.InvariantCulture) + ", " + Y.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
