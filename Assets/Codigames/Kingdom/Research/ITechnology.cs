using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Research
{
    // A one-time research: what it costs, where it sits on its book's page, what it needs before it, and what
    // it opens or moves.
    public interface ITechnology : IIdentifiable
    {
        // The book it is in.
        string Tome { get; }

        // Its band of the book, from 1; a chapter opens on revealed cells.
        int Era { get; }

        // Its slot on the page: a row is depth, three columns.
        int Row { get; }
        int Column { get; }

        // On a page at all: a technology left off the page is not in the game.
        bool IsPlaced { get; }

        // What must be researched first, all in the row above.
        IReadOnlyList<string> Requires { get; }

        // Poured in from the bar, on as many visits as it takes.
        double Knowledge { get; }

        // Paid once, when its Knowledge is in: Gold, and on some cards Wood, Stone or Food.
        IReadOnlyDictionary<string, double> Price { get; }

        TechKind Kind { get; }

        IReadOnlyList<TechUnlock> Unlocks { get; }

        IReadOnlyList<TechEffect> Effects { get; }

        // On the tree and researchable, but nothing reads it yet.
        bool Planned { get; }
    }
}
