using System.Collections.Generic;

namespace Codigames.Kingdom.Magic
{
    // The refills' numbers: the video's cooldown and when it is offered, the videos a day, the Gem ladder.
    public interface IRefillSettings
    {
        double CooldownMinSeconds { get; }
        double CooldownMaxSeconds { get; }
        // Below this share of the pool, a video is offered.
        double EligibleBelowFraction { get; }
        int RefillsPerDay { get; }
        // One price a rung, a day: when the rungs run out so do the purchases.
        IReadOnlyList<double> GemRefillCosts { get; }
    }
}
