using System.Collections.Generic;

namespace Codigames.Kingdom.Lairs
{
    // A tier's garrison: how many fights its path holds, how many seconds of the city's making one raid takes, and
    // what clearing it pays.
    public sealed class Garrison
    {
        public Garrison(int fights, double takeSeconds, double heroXp, IReadOnlyDictionary<string, int> rewardItems)
        {
            Fights = fights;
            TakeSeconds = takeSeconds;
            HeroXp = heroXp;
            RewardItems = rewardItems;
        }

        public int Fights { get; }
        public double TakeSeconds { get; }
        public double HeroXp { get; }
        // Bag items by id, and how many.
        public IReadOnlyDictionary<string, int> RewardItems { get; }
    }

    public interface ILairSettings
    {
        // By tier, 1 first.
        IReadOnlyList<Garrison> Garrisons { get; }

        // A raid takes at most this share of what the stores hold.
        double TakeFractionMax { get; }
        int RaidsPerDay { get; }
        // The local hours raids may land in.
        double WindowStartHour { get; }
        double WindowEndHour { get; }
        // The first fight on a lair's path, as a share of its guard's power.
        double FirstFightPower { get; }
        // Knowledge a lair's first clear pays.
        double FirstClearKnowledge { get; }
    }
}
