using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Map
{
    // What a long press on the map picks up (Docs/features/05-city-and-districts.md §4.3).
    public interface IMapHoldHandler
    {
        // Would a long press here pick something up? Asked before the ring shows.
        bool CanHold(ModuleVector2Int cell);

        // The press has been held: true when something was picked up, to be dragged by the same finger.
        bool Hold(ModuleVector2Int cell);
    }
}
