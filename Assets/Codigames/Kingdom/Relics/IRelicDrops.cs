using System.Collections.Generic;

namespace Codigames.Kingdom.Relics
{
    // Port: where the game pays relic fragments (a lair's claim, a treasure, a research band, a quest) without knowing
    // the relics.
    public interface IRelicDrops
    {
        IReadOnlyList<FragmentDrop> Drop(RelicKind? kind, int n, params object[] parts);

        IReadOnlyList<FragmentDrop> OpenDoor(string door);

        // A lair claimed: its door's first fragment, then the tier's share of the city relics met.
        IReadOnlyList<FragmentDrop> ForLair(string lair, int tier);

        // A treasure picked up: every so many, a city relic's fragment, rolled on its number.
        IReadOnlyList<FragmentDrop> ForTreasure(int n);
    }
}
