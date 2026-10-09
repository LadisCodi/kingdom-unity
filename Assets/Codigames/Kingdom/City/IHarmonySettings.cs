using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    public interface IHarmonySettings
    {
        // Ascending by ratio; the last one the city reaches pays.
        IReadOnlyList<HarmonyTier> SurplusTiers { get; }
    }
}
