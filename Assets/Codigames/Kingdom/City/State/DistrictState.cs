using Codigames.Modules.Core;

namespace Codigames.Kingdom.City.State
{
    // A building standing in the city, or being built.
    public class DistrictState : IIdentifiable
    {
        public string Id { get; set; }
        public string DefinitionId { get; set; }

        // Stamped when placed and never changed: the second Sawmill is #2 for life, and its price says so.
        public int Ordinal { get; set; }

        public int Level { get; set; }

        // Its top-left cell.
        public Vector2Int Anchor { get; set; }

        // False until its first build finishes.
        public bool Built { get; set; }
    }
}
