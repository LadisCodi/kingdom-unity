using Codigames.Game.Map;
using Codigames.Kingdom.City.State;
using UnityEngine;
using VContainer;

namespace Codigames.Game.City
{
    // Keeps the features layer in step with the ground: a feature gone from the ground (under the Townhall,
    // harvested away) is gone from the map.
    public class GroundView : MonoBehaviour
    {
        private GroundState _ground;
        private ProvinceMap _map;

        [Inject]
        public void Construct(GroundState ground, ProvinceMap map)
        {
            _ground = ground;
            _map = map;
        }

        private void Start() => Refresh();

        public void Refresh()
        {
            var layer = _map.Features;
            foreach (var position in layer.cellBounds.allPositionsWithin)
            {
                if (!layer.HasTile(position)) continue;
                if (!_ground.Features.ContainsKey(ProvinceCoordinates.FromTilemap(position))) layer.SetTile(position, null);
            }
        }
    }
}
