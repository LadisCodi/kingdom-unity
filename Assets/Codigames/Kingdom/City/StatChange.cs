namespace Codigames.Kingdom.City
{
    // A number the next level moves: what it is now, and the difference the level makes.
    public sealed class StatChange
    {
        public StatChange(BuildingStat now, double delta)
        {
            Now = now;
            Delta = delta;
        }

        public BuildingStat Now { get; }
        public double Delta { get; }

        // False when the level makes the number worse.
        public bool Better => Delta > 0 != Now.LowerIsBetter;
    }
}
