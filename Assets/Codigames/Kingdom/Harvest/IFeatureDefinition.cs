using Codigames.Modules.Core;

namespace Codigames.Kingdom.Harvest
{
    // Something standing on a cell (a stand of trees, a berry bush, a mountain) and what it yields.
    public interface IFeatureDefinition : IIdentifiable
    {
        // The harvest source it is; null when nothing can be taken from it.
        string Source { get; }

        // The terrain it comes back on when it respawns.
        string RespawnTerrain { get; }
    }
}
