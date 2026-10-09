using Codigames.Modules.Core;

namespace Codigames.Kingdom.City
{
    // Port of Construction: putting a feature on the ground (Docs/features/27-plantables.md), for what is bought to be
    // planted. The harvest owns the ground and implements it.
    public interface IPlanting
    {
        // How many of a feature stand on the ground: what prices and caps the next one planted.
        int Count(string feature);

        // Puts the feature on the cell, growing for its source's growth. The caller has said the ground is legal and
        // taken the price.
        void Plant(string feature, Vector2Int cell, double now);
    }
}
