namespace Codigames.Kingdom.Bag
{
    // Port: putting items in the Bag, for what pays in items (a quest's reward).
    public interface IItemGrants
    {
        void Grant(string itemId, int count);
    }
}
