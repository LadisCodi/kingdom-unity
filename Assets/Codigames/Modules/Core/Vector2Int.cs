using System;
using System.Globalization;

namespace Codigames.Modules.Core
{
    // A whole-number point in 2D: a cell of a grid. Ours, so it serialises exactly as written.
    [Serializable]
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        // Public fields, so every serialiser (Unity's included) writes the same two numbers.
        public int X;
        public int Y;

        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Vector2Int Zero => new(0, 0);

        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.Equals(b);
        public static bool operator !=(Vector2Int a, Vector2Int b) => !a.Equals(b);

        public bool Equals(Vector2Int other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is Vector2Int other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        // "x,y".
        public override string ToString()
            => X.ToString(CultureInfo.InvariantCulture) + "," + Y.ToString(CultureInfo.InvariantCulture);
    }
}
