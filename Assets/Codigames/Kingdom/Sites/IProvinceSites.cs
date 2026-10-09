using System.Collections.Generic;

namespace Codigames.Kingdom.Sites
{
    // What the province holds besides its ground, as authored.
    public interface IProvinceSites
    {
        IReadOnlyList<IAbandonedSite> Abandoned { get; }

        IReadOnlyList<ILandmarkSite> Landmarks { get; }
    }
}
