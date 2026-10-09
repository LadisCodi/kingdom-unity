using Codigames.Modules.Core;

namespace Codigames.Game.UI.Data
{
    // What the ghost is for: one more of a building, a building moving, or a tree or crop plot moving.
    public sealed class PlacementOrder
    {
        private PlacementOrder(string definitionId, string districtId, Vector2Int? featureCell)
        {
            DefinitionId = definitionId;
            DistrictId = districtId;
            FeatureCell = featureCell;
        }

        // The building built or moved; null for a feature.
        public string DefinitionId { get; }

        // The district moving; null otherwise.
        public string DistrictId { get; }

        // Where the tree or crop plot moving stands; null otherwise.
        public Vector2Int? FeatureCell { get; }

        public bool MovesDistrict => DistrictId != null;
        public bool MovesFeature => FeatureCell.HasValue;
        public bool IsMove => MovesDistrict || MovesFeature;

        public static PlacementOrder Build(string definitionId) => new(definitionId, null, null);

        public static PlacementOrder MoveDistrict(string districtId, string definitionId) => new(definitionId, districtId, null);

        public static PlacementOrder MoveFeature(Vector2Int cell) => new(null, null, cell);
    }
}
