using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Map
{
    // The province's cells and Unity's isometric tilemap cells. The province grows x east-south and y
    // west-south on screen, as the design draws it; Unity's isometric cell (a, b) sits at
    // ((a − b) / 2, (a + b) / 4) with y up. So a province cell (x, y) is the tilemap cell (−y, −x).
    public static class ProvinceCoordinates
    {
        public static UnityEngine.Vector3Int ToTilemap(ModuleVector2Int cell) => new(-cell.Y, -cell.X, 0);

        public static ModuleVector2Int FromTilemap(UnityEngine.Vector3Int cell) => new(-cell.y, -cell.x);
    }
}
