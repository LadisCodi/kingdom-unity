namespace Codigames.Kingdom.Fog
{
    // What a tap on the fog did, or why it did nothing (a refused tap costs nothing).
    public enum RevealResult
    {
        // A share of the price paid; the cell is not clear yet.
        Paid,
        // The last share: the cell is the kingdom's.
        Revealed,
        AlreadyRevealed,
        // Not next to revealed ground: the frontier stays connected.
        NotReachable,
        // Past the rings the Townhall's level allows.
        OutOfReach,
        NotEnoughGold,
    }
}
