namespace Codigames.Kingdom.Sites
{
    // Why an abandoned building cannot be repaired now.
    public enum RepairRefusal
    {
        None,
        NotFound,
        // The fog still covers some of it.
        NotRevealed,
        NoFreeBuilder,
        AtCap,
        CannotAfford,
        // It is missing a piece the Bag does not hold.
        MissingItem,
    }
}
