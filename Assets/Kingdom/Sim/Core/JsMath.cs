using System;

namespace Kingdom.Sim.Core
{
    // JavaScript's arithmetic where C#'s differs, so the port computes the very same numbers as the web
    // prototype. Use these, never Math.Round, for anything the sim decides.
    public static class JsMath
    {
        // Math.round: halves go UP (towards +∞), -2.5 → -2. C#'s Math.Round goes to the even neighbour.
        // Floor plus the fraction, not Floor(value + 0.5), which is wrong for 0.49999999999999994.
        public static double Round(double value)
        {
            if (!IsFinite(value)) return value;

            var floor = Math.Floor(value);
            return value - floor >= 0.5 ? floor + 1 : floor;
        }

        // Math.sign, with 0 → 0.
        public static double Sign(double value) => value > 0 ? 1 : value < 0 ? -1 : value;

        // Number.isFinite.
        public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
