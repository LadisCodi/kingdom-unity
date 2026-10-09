using Codigames.Modules.Core;

namespace Codigames.Kingdom.Harvest
{
    // A kind of ground that yields (a forest, a berry bush, a rock): what it pays and how its depot behaves.
    public interface IHarvestSource : IIdentifiable
    {
        // What it pays — not what it is: bushes, game and shoals all pay Food.
        string Currency { get; }

        // Units one extraction takes — the chunk.
        double UnitsPerStrike { get; }

        // Seconds one extraction takes — the rhythm. A tap pays its work seconds of chunk ÷ rhythm.
        double SecondsPerStrike { get; }

        // Units the cell holds when full, before the ground under it; 0 = bedrock, never runs out.
        double Stock { get; }

        // Seconds from empty back to full, in place; 0 = finite: emptied, the feature is gone.
        double RecoverySeconds { get; }

        // Finite sources: seconds until it appears again next to where it was; 0 = never.
        double RespawnSeconds { get; }
    }
}
