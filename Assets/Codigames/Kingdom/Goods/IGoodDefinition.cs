using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Goods
{
    // A refined good (Docs/features/17-workshops-and-goods.md §2): what one is made of — coins, another good, Mana —
    // and how long one villager works at it. A precious material is found, never made.
    public interface IGoodDefinition : IIdentifiable
    {
        string Name { get; }
        int Tier { get; }
        IReadOnlyDictionary<string, double> Input { get; }
        double InputMana { get; }
        // Another good it is made of, and how many; null for none.
        string InputGood { get; }
        int InputGoodAmount { get; }
        double WorkSeconds { get; }
        bool Precious { get; }
    }
}
