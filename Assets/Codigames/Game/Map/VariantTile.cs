using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Codigames.Game.Map
{
    // A tile of the province that stands for an id (a terrain, a feature) and draws one of its variants, the
    // same one at the same cell every time.
    [CreateAssetMenu(fileName = "Tile", menuName = "Kingdom/Map/Variant Tile")]
    public class VariantTile : TileBase
    {
        [SerializeField, Required] private string _id;
        [SerializeField, PreviewField] private Sprite[] _variants = new Sprite[0];

        public string Id => _id;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = _variants.Length == 0 ? null : _variants[Variant(ProvinceCoordinates.FromTilemap(position), _variants.Length)];
            tileData.color = Color.white;
            tileData.flags = TileFlags.LockTransform | TileFlags.LockColor;
            tileData.colliderType = Tile.ColliderType.None;
        }

        // A hash of the cell, so neighbours look different and a cell always looks the same.
        private static int Variant(Codigames.Modules.Core.Vector2Int cell, int count)
        {
            unchecked
            {
                var h = (uint)(cell.X * 374761393) ^ (uint)(cell.Y * 668265263);
                h = (h ^ (h >> 15)) * 2246822519u;
                return (int)(h % (uint)count);
            }
        }
    }
}
