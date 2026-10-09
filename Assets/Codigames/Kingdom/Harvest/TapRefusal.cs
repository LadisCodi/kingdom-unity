namespace Codigames.Kingdom.Harvest
{
    // Why a tap on the ground took nothing.
    public enum TapRefusal
    {
        None,
        // Nothing to take: bare ground, a building, outside the province.
        NothingThere,
        // Emptied, and growing back.
        Exhausted,
        NoMana,
        // A technology opens this source and is not researched yet: refused before any Mana is spent.
        NeedsResearch,
    }
}
