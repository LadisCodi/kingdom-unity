namespace Codigames.Kingdom.Fog.State
{
    // What the people who fled left on a cell: the kingdom's n-th find, and its coin.
    public class Treasure
    {
        public int N { get; set; }
        public string Coin { get; set; }

        // When it was set down.
        public double At { get; set; }
    }
}
