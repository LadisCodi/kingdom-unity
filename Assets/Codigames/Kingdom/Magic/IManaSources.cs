namespace Codigames.Kingdom.Magic
{
    // Port: what raises the Mana pool beyond its base — landmarks, a repaired Watchtower, the Sanctum.
    public interface IManaSources
    {
        double ExtraCap { get; }

        double ExtraPerHour { get; }
    }
}
