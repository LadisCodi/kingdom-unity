using Codigames.Modules.Core;

namespace Codigames.Kingdom.Fog
{
    public enum SightedKind
    {
        Mountain,
        Landmark,
        Abandoned,
        Lair,
    }

    // A tall thing seen past the fog: what it is (a mountain's feature, a landmark's or a ruin's id) and where.
    public readonly struct Sighted
    {
        public Sighted(SightedKind kind, string id, Vector2Int anchor, int size)
        {
            Kind = kind;
            Id = id;
            Anchor = anchor;
            Size = size;
        }

        public SightedKind Kind { get; }
        public string Id { get; }
        public Vector2Int Anchor { get; }
        public int Size { get; }

        public bool Covers(Vector2Int cell)
            => cell.X >= Anchor.X && cell.X < Anchor.X + Size && cell.Y >= Anchor.Y && cell.Y < Anchor.Y + Size;
    }
}
