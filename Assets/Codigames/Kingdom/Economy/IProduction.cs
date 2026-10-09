namespace Codigames.Kingdom.Economy
{
    // What the city makes of a coin a second, right now: the number every reward priced in production reads, so it
    // grows with the city instead of going stale.
    public interface IProduction
    {
        double MakesPerSecond(string currency);
    }
}
