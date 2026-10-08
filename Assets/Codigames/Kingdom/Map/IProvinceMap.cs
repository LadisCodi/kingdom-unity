using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Map
{
    // Port: the province as authored — which cells exist, the ground under each and what stands on it at the
    // start. Cells are the province's own coordinates: x grows east-south, y grows west-south on screen.
    public interface IProvinceMap
    {
        IEnumerable<Vector2Int> Cells { get; }

        bool Contains(Vector2Int cell);

        // The terrain's id ("Grassland", "Water"); null outside the province.
        string TerrainAt(Vector2Int cell);

        // The feature authored on the cell ("Trees", "Mountain"); null when it is bare.
        string FeatureAt(Vector2Int cell);
    }
}
