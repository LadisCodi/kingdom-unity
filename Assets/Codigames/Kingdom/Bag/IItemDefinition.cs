using Codigames.Kingdom.Economy;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Bag
{
    // An item of the Bag (Docs/proposals/inventory.md): held until it is used, never spent as a currency.
    public interface IItemDefinition : IIdentifiable
    {
        ItemKind Kind { get; }

        // A chest's coin; null for anything else (a choice chest's coin is picked when it is opened).
        string Coin { get; }

        // A chest's worth of production, a speed-up's cut, a boost's length; 0 for the rest.
        double Seconds { get; }

        // How rare it is, 1 to 5: the tile's frame.
        int Tier { get; }

        // What a speed-up fits; null for anything else.
        SpeedupKind? Speeds { get; }

        // What a boost raises; null for anything else.
        BoostKind? Boost { get; }

        // A boost's percent, a flask's share of the pool in percent, a tome's Knowledge.
        double Value { get; }
    }
}
