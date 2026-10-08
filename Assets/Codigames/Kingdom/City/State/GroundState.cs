using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.City.State
{
    // What stands on the ground now, by cell: the map's features as the game has changed them.
    public class GroundState
    {
        public Dictionary<Vector2Int, string> Features { get; set; } = new();
    }
}
