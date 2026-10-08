using System;
using System.Globalization;

namespace Codigames.Kingdom.Core
{
    // A cell of the province's square grid.
    public sealed class Coord : IEquatable<Coord>
    {
        public Coord()
        {
        }

        public Coord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }

        public int Y { get; set; }

        // "x,y": how the state keys a cell.
        public string Key => Of(X, Y);

        public static string Of(int x, int y)
            => x.ToString(CultureInfo.InvariantCulture) + "," + y.ToString(CultureInfo.InvariantCulture);

        public static Coord Parse(string key)
        {
            var comma = key.IndexOf(',');
            return new Coord(
                int.Parse(key.Substring(0, comma), CultureInfo.InvariantCulture),
                int.Parse(key.Substring(comma + 1), CultureInfo.InvariantCulture));
        }

        public bool Equals(Coord other) => other != null && X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is Coord other && Equals(other);

        public override int GetHashCode() => unchecked(X * 397 ^ Y);

        public override string ToString() => Key;
    }
}
