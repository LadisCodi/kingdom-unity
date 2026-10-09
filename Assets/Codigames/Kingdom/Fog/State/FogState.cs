using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Fog.State
{
    // The province as the kingdom sees it: what is its own, what it has seen, and how far each cell being
    // cleared has got.
    public class FogState
    {
        public HashSet<Vector2Int> Revealed { get; set; } = new();

        // Seen from afar (a building's ring) without being next to revealed ground.
        public HashSet<Vector2Int> Discovered { get; set; } = new();

        // Taps already paid on a cell being cleared.
        public Dictionary<Vector2Int, int> Progress { get; set; } = new();
    }
}
