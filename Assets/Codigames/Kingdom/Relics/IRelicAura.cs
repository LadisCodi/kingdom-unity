using Codigames.Kingdom.City.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Relics
{
    // Port: what the awake city relics do to a number — on a cell of ground, or over a building, which is in an aura
    // as a whole when any of its cells is. A multiplier: 1 where no aura reaches.
    public interface IRelicAura
    {
        double At(string stat, Vector2Int cell);

        double Over(string stat, DistrictState district);
    }

    public static class RelicAuraExtensions
    {
        public static double At(this IRelicAura aura, string stat, Vector2Int cell) => aura == null ? 1 : aura.At(stat, cell);

        public static double Over(this IRelicAura aura, string stat, DistrictState district) => aura == null ? 1 : aura.Over(stat, district);
    }
}
