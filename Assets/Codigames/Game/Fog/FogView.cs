using System.Collections.Generic;
using Codigames.Game.Map;
using Codigames.Kingdom.Fog;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Fog
{
    // The fog as a sunlit sea of clouds: undiscovered ground under the bank (its floor in the cloud's tone, a
    // cloud standing on it), ground the player can clear under a thin veil and a faint patch of cloud, ground
    // seen but out of reach under a cushion. The bank runs on past the province's edge. Revealed ground is clear.
    public class FogView : MonoBehaviour
    {
        [SerializeField] private Tilemap _floor;
        [SerializeField] private Tilemap _clouds;
        [SerializeField] private TileBase _bankFloor;
        [SerializeField] private TileBase _veil;
        [SerializeField] private TileBase _bankCloud;
        [SerializeField] private TileBase _patch;
        [SerializeField] private TileBase _cushion;
        [SerializeField, Tooltip("Cells of cloud bank past the province's edge.")] private int _margin = 4;

        private FogOfWar _fog;
        private ProvinceMap _map;
        private BoundsInt _bounds;

        [Inject]
        public void Construct(FogOfWar fog, ProvinceMap map)
        {
            _fog = fog;
            _map = map;
        }

        private void Start()
        {
            var province = _map.Terrain.cellBounds;
            _bounds = new BoundsInt(province.xMin - _margin, province.yMin - _margin, 0,
                province.size.x + 2 * _margin, province.size.y + 2 * _margin, 1);

            Refresh();
            _fog.Changed += OnChanged;
        }

        private void OnDestroy()
        {
            if (_fog != null) _fog.Changed -= OnChanged;
        }

        // A reveal can change what can be paid for far away (the reach), so the whole fog is drawn again.
        private void OnChanged(IReadOnlyCollection<ModuleVector2Int> cells) => Refresh();

        private void Refresh()
        {
            var count = _bounds.size.x * _bounds.size.y;
            var positions = new Vector3Int[count];
            var floors = new TileBase[count];
            var clouds = new TileBase[count];

            var i = 0;
            foreach (var position in _bounds.allPositionsWithin)
            {
                positions[i] = position;
                var cell = ProvinceCoordinates.FromTilemap(position);

                if (!_map.Contains(cell))
                {
                    floors[i] = _bankFloor;
                    clouds[i] = _bankCloud;
                }
                else
                {
                    switch (_fog.VisibilityAt(cell))
                    {
                        case Visibility.Undiscovered:
                            floors[i] = _bankFloor;
                            clouds[i] = _bankCloud;
                            break;
                        case Visibility.Discovered:
                            floors[i] = _veil;
                            clouds[i] = _fog.IsPayable(cell) ? _patch : _cushion;
                            break;
                    }
                }

                i++;
            }

            _floor.SetTiles(positions, floors);
            _clouds.SetTiles(positions, clouds);
        }
    }
}
