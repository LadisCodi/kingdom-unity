using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Map;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;

namespace Codigames.Kingdom.City
{
    // A new kingdom's city: the Townhall standing at level 1 on the province's origin, the map's features
    // on the ground (save under the Townhall), and the builders it starts with.
    public static class NewCity
    {
        public static readonly Vector2Int TOWNHALL_ORIGIN = new(0, 0);

        public static (CityState City, GroundState Ground) Create(IProvinceMap map, IConstructionSettings settings)
        {
            var townhall = settings.Townhall;
            var city = new CityState { Builders = settings.StartBuilders };
            city.Districts.Add(new DistrictState
            {
                Id = city.NewId(Construction.DISTRICT_PREFIX),
                DefinitionId = townhall.Id,
                Ordinal = 1,
                Level = 1,
                Anchor = TOWNHALL_ORIGIN,
                Built = true,
            });

            var ground = new GroundState();
            foreach (var cell in map.Cells)
            {
                var feature = map.FeatureAt(cell);
                if (feature != null) ground.Features[cell] = feature;
            }

            foreach (var cell in GridMath.Rect(TOWNHALL_ORIGIN, townhall.Width, townhall.Height)) ground.Features.Remove(cell);

            return (city, ground);
        }
    }
}
