namespace Codigames.Kingdom.Goods
{
    // Whether precious materials are in play: until the Watchtower is claimed they are no part of any price.
    public interface IPreciousGate
    {
        bool Open { get; }
    }

    // The gate while claiming the Watchtower is not in the game: shut.
    public sealed class ShutPreciousGate : IPreciousGate
    {
        public bool Open => false;
    }
}
