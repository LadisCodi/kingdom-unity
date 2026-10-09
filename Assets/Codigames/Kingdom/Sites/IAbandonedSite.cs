using Codigames.Modules.Core;

namespace Codigames.Kingdom.Sites
{
    // A building the fog swallowed, standing in ruin where it was: authored once, the same for every kingdom.
    public interface IAbandonedSite : IIdentifiable
    {
        // The building it is.
        string District { get; }

        // Its top-left cell.
        Vector2Int Anchor { get; }

        // How far from revealed ground its ruin shows as a silhouette.
        int Sight { get; }

        // What its card and banner call it.
        string Name { get; }
    }
}
