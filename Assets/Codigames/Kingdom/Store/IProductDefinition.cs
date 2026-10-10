using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Store
{
    // Where a product is sold: the Gem packs, the Bag's bundles, an offer's window, today's draw, the Survey.
    public enum ProductShelf
    {
        Gems,
        Bag,
        Offer,
        Daily,
        Survey,
    }

    // A product sold for (simulated) money (Docs/features/14-monetization.md §2): its price, the Gems it lands, what
    // else it hands over — items, a hero, slots for good — its next-day part, and, for an offer, what opens it, for
    // how long and how often.
    public interface IProductDefinition : IIdentifiable
    {
        string DisplayName { get; }
        string Description { get; }
        ProductShelf Shelf { get; }
        double PriceUsd { get; }
        int Gems { get; }
        IReadOnlyDictionary<string, int> Items { get; }
        // A hero handed over; null for none.
        string Hero { get; }
        int Builders { get; }
        int Explorers { get; }
        int HeroSlots { get; }

        // The part that waits for the next day.
        int NextDayGems { get; }
        int NextDayHeroXp { get; }
        int NextDayFragments { get; }
        IReadOnlyDictionary<string, int> NextDayItems { get; }

        // An offer's window: what opens it (always, door, after, townhall, manaLow…), its gates, its hours (0: until
        // sold out), its limit (0: none) and the wait before a repeating trigger opens it again.
        string OpensOn { get; }
        string Door { get; }
        string After { get; }
        int Townhall { get; }
        double Hours { get; }
        int Limit { get; }
        double CooldownHours { get; }
        bool Splash { get; }
        bool Widget { get; }
    }
}
