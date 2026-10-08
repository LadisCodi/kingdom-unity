using UnityEngine;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Map
{
    // Where things stand on the province: a footprint's diamond, and the point a standing sprite rests on.
    public static class ProvinceGeometry
    {
        // The bottom centre of a footprint's bounding diamond, and its width: a standing sprite is that wide and
        // rests on that point, as the web drew it.
        public static (Vector3 Base, float Width) Footprint(ProvinceMap map, ModuleVector2Int anchor, int width, int height)
        {
            var cell = map.Grid.cellSize;
            var bottom = map.CellCentre(new ModuleVector2Int(anchor.X + width - 1, anchor.Y + height - 1));
            var left = map.CellCentre(new ModuleVector2Int(anchor.X, anchor.Y + height - 1));
            var right = map.CellCentre(new ModuleVector2Int(anchor.X + width - 1, anchor.Y));

            var leftX = left.x - cell.x / 2f;
            var rightX = right.x + cell.x / 2f;
            return (new Vector3((leftX + rightX) / 2f, bottom.y - cell.y / 2f, 0f), rightX - leftX);
        }
    }
}
