using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City.State;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;

namespace Codigames.Kingdom.City
{
    // Facts about a city, read off its state and the buildings' definitions.
    public static class CityQueries
    {
        public static DistrictState Townhall(CityState city, IConstructionSettings settings)
            => city.Districts.FirstOrDefault(d => d.DefinitionId == settings.Townhall.Id);

        // The era: the Townhall's level, 1 before it has one.
        public static int TownhallLevel(CityState city, IConstructionSettings settings) => Townhall(city, settings)?.Level ?? 1;

        public static int Count(CityState city, string definitionId) => city.Districts.Count(d => d.DefinitionId == definitionId);

        // How many of a building may stand at the Townhall's level; null = unlimited.
        public static int? MaxCount(IBuildingDefinition building, int townhallLevel)
        {
            var caps = building.Gates.MaxCountPerTownhallLevel;
            if (caps.Count == 0) return null;
            return caps[System.Math.Min(System.Math.Max(townhallLevel, 1), caps.Count) - 1];
        }

        public static IEnumerable<Vector2Int> Footprint(DistrictState district, IBuildingDefinition building)
            => GridMath.Rect(district.Anchor, building.Width, building.Height);

        public static DistrictState At(CityState city, ICatalog<IBuildingDefinition> buildings, Vector2Int cell)
            => city.Districts.FirstOrDefault(d => Footprint(d, buildings.Get(d.DefinitionId)).Contains(cell));

        // Rings from the Townhall's footprint to a cell: 0 inside it.
        public static int DistanceFromTownhall(CityState city, ICatalog<IBuildingDefinition> buildings, IConstructionSettings settings, Vector2Int cell)
        {
            var townhall = Townhall(city, settings);
            if (townhall == null) return 0;

            var definition = buildings.Get(townhall.DefinitionId);
            return GridMath.ChebyshevToRect(cell, townhall.Anchor, definition.Width, definition.Height);
        }

        public static int BusyBuilders(CityState city) => city.Jobs.Count;

        public static bool HasFreeBuilder(CityState city) => city.Jobs.Count < city.Builders;
    }
}
