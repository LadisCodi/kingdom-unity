namespace Codigames.Kingdom.Harvest
{
    // What the ground under a cell does to what it holds: a multiplier per terrain and currency on its stock.
    public interface ITerrainYields
    {
        double YieldOf(string terrain, string currency);
    }
}
