using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    // One level's price: currencies (multiplied by the instance) and refined goods (never multiplied).
    public interface ILevelCost
    {
        IReadOnlyDictionary<string, double> Currencies { get; }
        IReadOnlyDictionary<string, double> Goods { get; }
    }
}
