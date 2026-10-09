using System.Collections.Generic;

namespace Codigames.Kingdom.Fog
{
    public interface ITreasureSettings
    {
        // One is due every this many cells paid for, the first on the very first.
        int EveryReveals { get; }

        // What one pays: this many seconds of what the city makes of its coin…
        double WorkSeconds { get; }

        // …never less than this, by coin.
        IReadOnlyDictionary<string, double> Floor { get; }

        // How likely each coin is, Stone only once Stone is on the plank.
        IReadOnlyDictionary<string, double> Weights { get; }

        // A Knowledge find pays this, fixed.
        double Knowledge { get; }

        // The first find is fixed, for the opening.
        string FirstCoin { get; }
        double FirstAmount { get; }
    }
}
