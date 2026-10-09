namespace Codigames.Kingdom.City
{
    // What repairing an abandoned one of a building asks: its own wait, and a piece the ruin is missing.
    public interface IBuildingRepair
    {
        // Seconds; 0 waits as long as a build does.
        double Seconds { get; }

        // The item it is missing; empty when it misses nothing.
        string Item { get; }
    }
}
