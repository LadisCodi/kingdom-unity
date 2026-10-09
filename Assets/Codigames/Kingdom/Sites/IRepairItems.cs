namespace Codigames.Kingdom.Sites
{
    // Port: the pieces a ruin may be missing, held in the Bag.
    public interface IRepairItems
    {
        int Held(string item);

        void Take(string item);
    }
}
