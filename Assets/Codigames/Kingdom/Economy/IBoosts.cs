namespace Codigames.Kingdom.Economy
{
    // Port: what the boosts running now multiply (1 with none). Rent, taps and the Mana pool each read their own.
    public interface IBoosts
    {
        double Multiplier(BoostKind kind);
    }
}
