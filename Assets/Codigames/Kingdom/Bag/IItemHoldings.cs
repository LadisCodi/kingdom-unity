namespace Codigames.Kingdom.Bag
{
    // Port: what the Bag holds of an item and taking some out, for what spends items (a call spends a key).
    public interface IItemHoldings : IItemGrants
    {
        int Count(string itemId);

        // Takes `count` out; false, and nothing taken, when fewer are held.
        bool Take(string itemId, int count);
    }
}
