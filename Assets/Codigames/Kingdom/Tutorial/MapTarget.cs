using Codigames.Modules.Core;

namespace Codigames.Kingdom.Tutorial
{
    // A plot of the map a line points at: its top-left cell and its size in cells.
    public readonly struct MapTarget
    {
        public MapTarget(Vector2Int anchor, int width = 1, int height = 1)
        {
            Anchor = anchor;
            Width = width;
            Height = height;
        }

        public Vector2Int Anchor { get; }
        public int Width { get; }
        public int Height { get; }

        public bool Contains(Vector2Int cell)
            => cell.X >= Anchor.X && cell.X < Anchor.X + Width && cell.Y >= Anchor.Y && cell.Y < Anchor.Y + Height;
    }
}
