using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Lairs
{
    // A lair of the province (Docs/features/18-garrisons-and-raids.md §2): where it stands, how far its ground reaches,
    // its tier, and the guard that holds it — the unit it fields most, its power, the warning before its first raid.
    public interface ILairSite : IIdentifiable
    {
        string Name { get; }
        string Description { get; }
        string Flavour { get; }
        Vector2Int Anchor { get; }
        int Size { get; }
        int Tier { get; }
        int Radius { get; }
        int Sight { get; }
        // The unit it fields most; "Any" for a drake.
        string Threat { get; }
        int Power { get; }
        double WarningMinutes { get; }
        IReadOnlyDictionary<string, double> Mix { get; }
        // What its garrison is rolled under: the event the roll is keyed on, kept when the lair is renamed so a garrison
        // the player has looked at is never re-rolled.
        string RollKey { get; }
    }
}
