using Codigames.Modules.Core;

namespace Codigames.Kingdom.City
{
    // Port: ground a site of the province stands on, where nothing else may be placed.
    public interface ISiteGround
    {
        bool Holds(Vector2Int cell);
    }
}
