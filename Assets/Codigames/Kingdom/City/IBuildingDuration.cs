namespace Codigames.Kingdom.City
{
    // How long building and upgrading take:
    //   build      = seconds × districtGrowth^(N − 1) × distanceGrowth^d
    //   upgrade(L) = seconds × levelGrowth^(L − 2), and from the late pivot lateSeconds × lateGrowth^(L − pivot).
    public interface IBuildingDuration
    {
        double BuildSeconds { get; }
        double BuildCountGrowth { get; }
        double BuildDistanceGrowth { get; }
        double UpgradeSeconds { get; }
        double UpgradeLevelGrowth { get; }

        // 0 = no late levels: the early curve simply continues.
        double LateUpgradeSeconds { get; }
        double LateUpgradeLevelGrowth { get; }
    }
}
