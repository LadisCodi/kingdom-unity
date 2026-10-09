using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Harvest.State
{
    // The ground as the thumb has left it: depots drawn on, fractions owed, features waiting to come back.
    public class HarvestState
    {
        public Dictionary<Vector2Int, CellDepot> Depots { get; set; } = new();

        // The fraction of a unit a tap owed but did not pay, per currency, paid with a later tap.
        public Dictionary<string, double> Carry { get; set; } = new();

        public List<Respawn> Respawns { get; set; } = new();

        // Respawns are numbered in order, so each one's random draw is its own.
        public int NextRespawn { get; set; } = 1;
    }
}
