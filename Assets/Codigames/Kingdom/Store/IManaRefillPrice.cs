namespace Codigames.Kingdom.Store
{
    // What the first Gem refill of the Mana pool costs: a Mana flask is worth its share of it.
    public interface IManaRefillPrice
    {
        double FirstRefillGems { get; }
    }
}
