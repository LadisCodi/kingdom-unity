namespace Codigames.Kingdom.Sites
{
    public enum ClaimResult
    {
        Claimed,
        NotFound,
        AlreadyClaimed,
        // The fog still covers it.
        NotRevealed,
        // A lair still holds its ground.
        LairHeld,
        CannotAfford,
    }
}
