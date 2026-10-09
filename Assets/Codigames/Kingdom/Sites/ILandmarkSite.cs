using Codigames.Modules.Core;

namespace Codigames.Kingdom.Sites
{
    // A sanctuary of the province, claimed once for its Gold: standing stones, a leyspring.
    public interface ILandmarkSite : IIdentifiable
    {
        string Kind { get; }

        Vector2Int Anchor { get; }

        // Its side, in cells.
        int Size { get; }

        // Gold to claim it, authored per site.
        double ClaimCost { get; }
    }
}
