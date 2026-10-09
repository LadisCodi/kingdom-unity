using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Map
{
    // Something on the map a finger can pick up and drag cell by cell (a building being placed). While it holds
    // the finger, the camera does not pan.
    public interface IMapDragHandler
    {
        // A finger came down on a cell: true takes the drag.
        bool BeginDrag(ModuleVector2Int cell);

        void Drag(ModuleVector2Int cell);

        void EndDrag();
    }
}
