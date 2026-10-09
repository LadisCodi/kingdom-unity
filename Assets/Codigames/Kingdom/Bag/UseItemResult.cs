namespace Codigames.Kingdom.Bag
{
    public enum UseItemResult
    {
        Used,
        NotHeld,
        UnknownItem,
        // A speed-up is used on a timer, a key on a call, a part by a repair.
        UsedElsewhere,
        // A choice chest is opened for a coin.
        NeedsACoin,
    }
}
