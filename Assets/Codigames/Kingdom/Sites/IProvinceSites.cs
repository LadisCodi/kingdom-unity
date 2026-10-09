using System.Collections.Generic;

namespace Codigames.Kingdom.Sites
{
    // What the province holds besides its ground, as authored.
    public interface IProvinceSites
    {
        IReadOnlyList<IAbandonedSite> Abandoned { get; }

        IReadOnlyList<ILandmarkSite> Landmarks { get; }

        // The lairs, in their order: the order raids resolve in when two land together.
        IReadOnlyList<Lairs.ILairSite> Lairs => System.Array.Empty<Lairs.ILairSite>();
    }
}
