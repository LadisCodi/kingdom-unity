namespace Codigames.Kingdom.Bag
{
    // What using an item does. A speed-up is used on a timer, a key on its banner's call and a part by the repair
    // that needs it — never from the Bag alone.
    public enum ItemKind
    {
        Chest,
        Choice,
        Speedup,
        Boost,
        Flask,
        Tome,
        Key,
        Part,
    }
}
