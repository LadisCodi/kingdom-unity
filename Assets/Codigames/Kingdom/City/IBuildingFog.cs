using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    // What a finished building does to the fog: the ground it reveals round its footprint and the ring it
    // discovers past that.
    public interface IBuildingFog
    {
        int RevealRadius { get; }

        // By level, from 1, when the level changes it (the Townhall: 1, then 3); empty = RevealRadius at every level.
        IReadOnlyList<int> RevealRadiusPerLevel { get; }

        int DiscoverRadius { get; }
    }
}
