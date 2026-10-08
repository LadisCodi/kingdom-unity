using System;

namespace Kingdom.Sim.Core
{
    public static class Prices
    {
        // A number a player can read back: every CALCULATED cost and reward is rounded to three significant
        // figures before it is charged or paid (1,234 → 1,230), a whole number below 1,000. An authored
        // number never passes through here.
        public static double RoundPrice(double n)
        {
            if (!JsMath.IsFinite(n)) return n;

            var sign = n < 0 ? -1 : 1;
            var v = Math.Abs(n);
            if (v < 1000) return sign * JsMath.Round(v);

            var scale = Math.Pow(10, Math.Floor(Math.Log10(v)) - 2);
            return sign * JsMath.Round(v / scale) * scale;
        }
    }
}
