using System;
using System.Globalization;
using System.Text;

namespace Codigames.Modules.Randomness
{
    // Counter/hash randomness, never a stream: a draw is a pure function of the seed and the parts that
    // identify the EVENT (a lair and a depth, a banner and a pull number), so the offline replay may group
    // its work however it likes and a new consumer shifts no other draw. Bit-identical to the web
    // prototype's rng.ts.
    public static class Rand
    {
        // Never produced by an id or a coordinate key, so ("ab", "c") and ("a", "bc") cannot collide.
        private const char SEPARATOR = '\u001f';
        private const double TWO_POW_32 = 4294967296.0;

        // A uniform value in [0, 1).
        public static double Value(uint seed, params object[] parts) => Hash32(Join(parts), seed) / TWO_POW_32;

        // A uniform integer in [0, max). Zero when max <= 0.
        public static int Int(uint seed, int max, params object[] parts)
            => max <= 0 ? 0 : (int)(Hash32(Join(parts), seed) % (uint)max);

        public static T Pick<T>(uint seed, T[] items, params object[] parts) => items[Int(seed, items.Length, parts)];

        public static bool Chance(uint seed, double probability, params object[] parts)
            => Value(seed, parts) < probability;

        // FNV-1a-flavoured mixing with a final avalanche, all in 32-bit space.
        private static uint Hash32(string text, uint seed)
        {
            unchecked
            {
                var h = seed ^ 0x811c9dc5u;

                foreach (var c in text)
                {
                    h ^= c;
                    h *= 0x01000193u;
                }

                h ^= h >> 16;
                h *= 0x7feb352du;
                h ^= h >> 15;
                h *= 0x846ca68bu;
                return h ^ (h >> 16);
            }
        }

        private static string Join(object[] parts)
        {
            if (parts.Length == 0) return string.Empty;

            var text = new StringBuilder();

            for (var i = 0; i < parts.Length; i++)
            {
                if (i > 0) text.Append(SEPARATOR);
                text.Append(Format(parts[i]));
            }

            return text.ToString();
        }

        // Only what the web prototype writes the same way: a fractional number would print differently.
        private static string Format(object part) => part switch
        {
            string s => s,
            int n => n.ToString(CultureInfo.InvariantCulture),
            long n => n.ToString(CultureInfo.InvariantCulture),
            uint n => n.ToString(CultureInfo.InvariantCulture),
            _ => throw new ArgumentException($"A random part must be a string or an integer, not {part?.GetType().Name ?? "null"}."),
        };
    }
}
