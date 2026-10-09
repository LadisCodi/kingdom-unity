using System.Collections.Generic;

namespace Codigames.Kingdom.Research
{
    // The shelf: which books there are, in order, and each book's bands.
    public interface ITechTree
    {
        // The books, the Kingdom's tree first.
        IReadOnlyList<string> Tomes { get; }

        // How many bands a book has.
        int Eras(string tome);

        // Revealed cells a band asks for before it opens; band 1 asks for none.
        int CellsToOpen(string tome, int era);

        // What researching every card of a band pays, once: relic fragments, or null for nothing.
        int? EraReward(string tome, int era);
    }
}
