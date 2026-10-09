namespace Codigames.Kingdom.Magic
{
    public interface IManaSettings
    {
        // The pool's size with nothing raising it.
        double BaseCap { get; }

        // Mana the pool gains an hour with nothing raising it.
        double BasePerHour { get; }

        // What a landmark claimed (or the Watchtower repaired) adds to the pool, for good.
        double LandmarkCap { get; }
    }
}
