using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    // What every level costs the first instance (level 1 is the build), and how much dearer a later one is:
    // M(N) = linear × (N − 1) + growth^(N − 1), on the currencies only.
    public interface IBuildingCost
    {
        // Exactly one entry per level.
        IReadOnlyList<ILevelCost> PerLevel { get; }

        double InstanceLinearGrowth { get; }
        double InstanceExponentialGrowth { get; }
    }
}
