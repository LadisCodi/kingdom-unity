using Codigames.Kingdom.City;

namespace Codigames.Kingdom.Fog
{
    // A building finishing — built, or a level that widens its rings (the Townhall at level 2) — lifts the fog
    // round it.
    public class BuildingsLiftFog
    {
        public BuildingsLiftFog(Construction construction, FogOfWar fog)
        {
            construction.JobCompleted += (job, district) => fog.RevealAround(district);
        }
    }
}
