namespace Codigames.Kingdom.Economy
{
    public interface IEconomySettings
    {
        // The rent every housed villager pays.
        double GoldPerPopulationPerMinute { get; }

        // A store is ready to collect once it holds this many seconds of what its building makes now.
        double CollectSeconds { get; }
    }
}
