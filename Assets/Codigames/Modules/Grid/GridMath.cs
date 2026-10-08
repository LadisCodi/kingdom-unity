using System;
using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Modules.Grid
{
    // The geometry of a square grid: who is next to whom, how far apart two cells are, which cells an area
    // covers. Three distances coexist on purpose — adjacency is 4-way, a radius is a Chebyshev square, a walk
    // is Euclidean.
    public static class GridMath
    {
        private static readonly Vector2Int[] SIDES = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };

        // The four cells sharing a side, north first and clockwise. Diagonals are not neighbours.
        public static IEnumerable<Vector2Int> Neighbours(Vector2Int cell)
        {
            foreach (var side in SIDES) yield return cell + side;
        }

        public static bool AreNeighbours(Vector2Int a, Vector2Int b) => Manhattan(a, b) == 1;

        public static int Chebyshev(Vector2Int a, Vector2Int b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        public static int Manhattan(Vector2Int a, Vector2Int b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        public static double Euclidean(Vector2Int a, Vector2Int b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // The cells of a width × height rectangle whose top-left cell is the anchor, row by row.
        public static IEnumerable<Vector2Int> Rect(Vector2Int anchor, int width, int height)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++) yield return new Vector2Int(anchor.X + x, anchor.Y + y);
            }
        }

        // The Chebyshev distance from a cell to the nearest cell of a rectangle (0 inside it).
        public static int ChebyshevToRect(Vector2Int cell, Vector2Int anchor, int width, int height)
        {
            var dx = Math.Max(0, Math.Max(anchor.X - cell.X, cell.X - (anchor.X + width - 1)));
            var dy = Math.Max(0, Math.Max(anchor.Y - cell.Y, cell.Y - (anchor.Y + height - 1)));
            return Math.Max(dx, dy);
        }

        // Every cell within a Chebyshev radius of a rectangle (radius 0 = the rectangle itself).
        public static IEnumerable<Vector2Int> AroundRect(Vector2Int anchor, int width, int height, int radius)
            => Rect(new Vector2Int(anchor.X - radius, anchor.Y - radius), width + 2 * radius, height + 2 * radius);
    }
}
