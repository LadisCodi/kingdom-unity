using Codigames.Modules.Core;

namespace Codigames.Kingdom.Fog
{
    // Whether a cell is the kingdom's own ground: what placing and harvesting ask of the fog.
    public interface IRevealedGround
    {
        bool IsRevealed(Vector2Int cell);
    }
}
