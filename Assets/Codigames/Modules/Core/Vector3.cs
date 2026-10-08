using System;
using System.Globalization;

namespace Codigames.Modules.Core
{
    // A point or a direction in 3D, ours rather than an engine's or a library's, so it serialises exactly as
    // written: three public fields, X, Y and Z. Game converts it to and from Unity's at the edge.
    [Serializable]
    public struct Vector3 : IEquatable<Vector3>
    {
        // Public fields, so every serialiser (Unity's included) writes the same three numbers.
        public float X;
        public float Y;
        public float Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(Vector2 xy, float z) : this(xy.X, xy.Y, z)
        {
        }

        public static Vector3 Zero => new(0f, 0f, 0f);
        public static Vector3 One => new(1f, 1f, 1f);

        public Vector2 XY => new(X, Y);
        public float SqrMagnitude => X * X + Y * Y + Z * Z;
        public float Magnitude => MathF.Sqrt(SqrMagnitude);

        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator -(Vector3 a) => new(-a.X, -a.Y, -a.Z);
        public static Vector3 operator *(Vector3 a, float k) => new(a.X * k, a.Y * k, a.Z * k);
        public static Vector3 operator *(float k, Vector3 a) => new(a.X * k, a.Y * k, a.Z * k);
        public static Vector3 operator /(Vector3 a, float k) => new(a.X / k, a.Y / k, a.Z / k);
        public static bool operator ==(Vector3 a, Vector3 b) => a.Equals(b);
        public static bool operator !=(Vector3 a, Vector3 b) => !a.Equals(b);

        // Linear interpolation, t clamped to [0, 1].
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new Vector3(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);
        }

        public static float Distance(Vector3 a, Vector3 b) => (a - b).Magnitude;

        public bool Equals(Vector3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

        public override bool Equals(object obj) => obj is Vector3 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public override string ToString()
            => "(" + X.ToString(CultureInfo.InvariantCulture) + ", " + Y.ToString(CultureInfo.InvariantCulture) + ", "
               + Z.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
