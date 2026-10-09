using Codigames.Modules.Core;

namespace Codigames.Kingdom.Harvest.State
{
    // A finite feature that was emptied, waiting to come back next to where it stood.
    public class Respawn
    {
        public string Id { get; set; }
        public string FeatureId { get; set; }
        public Vector2Int Origin { get; set; }

        // Epoch milliseconds.
        public double DueAt { get; set; }
    }
}
