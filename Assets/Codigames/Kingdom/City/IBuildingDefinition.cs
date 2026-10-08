using Codigames.Modules.Core;

namespace Codigames.Kingdom.City
{
    // A kind of building: its shape, and what it costs, how long it takes and what gates it, each its own part
    // so a rule reads only the part it needs.
    public interface IBuildingDefinition : IIdentifiable
    {
        int MaxLevel { get; }

        // The footprint in cells; the anchor is its top-left cell.
        int Width { get; }
        int Height { get; }

        // False for what the city starts with (the Townhall).
        bool Buildable { get; }

        IBuildingCost Cost { get; }
        IBuildingDuration Duration { get; }
        IBuildingGates Gates { get; }
    }
}
