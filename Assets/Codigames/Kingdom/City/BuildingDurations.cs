using System;

namespace Codigames.Kingdom.City
{
    // How long a builder takes:
    //   build      = seconds × countGrowth^(instances already standing) × distanceGrowth^(rings from the Townhall)
    //   upgrade(L) = seconds × levelGrowth^(L − 2), or from the late pivot lateSeconds × lateGrowth^(L − pivot)
    public static class BuildingDurations
    {
        public static double BuildSeconds(IBuildingDuration duration, int alreadyStanding, int ringsFromTownhall)
            => Math.Round(duration.BuildSeconds
                          * Math.Pow(duration.BuildCountGrowth, alreadyStanding)
                          * Math.Pow(duration.BuildDistanceGrowth, ringsFromTownhall), MidpointRounding.AwayFromZero);

        public static double UpgradeSeconds(IBuildingDuration duration, int targetLevel, int lateFromLevel)
        {
            var seconds = targetLevel < lateFromLevel || duration.LateUpgradeSeconds <= 0
                ? duration.UpgradeSeconds * Math.Pow(duration.UpgradeLevelGrowth, targetLevel - 2)
                : duration.LateUpgradeSeconds * Math.Pow(duration.LateUpgradeLevelGrowth, targetLevel - lateFromLevel);
            return Math.Round(seconds, MidpointRounding.AwayFromZero);
        }
    }
}
