using System.Collections.Generic;

namespace Codigames.Kingdom.Fog
{
    // How far tall things are seen from past the fog, in rings of revealed ground.
    public interface ISightSettings
    {
        // A mountain block by its side, from 1; 0 is never.
        IReadOnlyList<int> MountainBySize { get; }

        int Landmark { get; }
    }
}
