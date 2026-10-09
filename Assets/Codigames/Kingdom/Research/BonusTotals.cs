namespace Codigames.Kingdom.Research
{
    // What the tree adds to a number (in its own units) and scales it by (as a fraction: 0.15 for +15%).
    public readonly struct BonusTotals
    {
        public static readonly BonusTotals None = new(0, 0);

        public BonusTotals(double flat, double percent)
        {
            Flat = flat;
            Percent = percent;
        }

        public double Flat { get; }
        public double Percent { get; }

        public double Multiplier => 1 + Percent;

        public double Apply(double value) => (value + Flat) * (1 + Percent);

        public static BonusTotals operator +(BonusTotals a, BonusTotals b) => new(a.Flat + b.Flat, a.Percent + b.Percent);
    }
}
