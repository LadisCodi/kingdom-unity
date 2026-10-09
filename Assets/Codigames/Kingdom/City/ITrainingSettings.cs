using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    // Villagers trained at the Townhall: what each one costs and how long each one takes.
    public interface ITrainingSettings
    {
        // What a villager costs, by their place in the town (population plus everyone queued), from the first.
        IReadOnlyList<double> CostFirst { get; }

        // Past that list, each villager costs this many times the one before.
        double CostGrowth { get; }

        // The first villager's wait, and how much longer each place in the town makes it.
        double Seconds { get; }
        double SecondsGrowth { get; }
    }
}
