using System;

namespace Codigames.Kingdom.Army
{
    // A troop is a unit at a rank: "Warrior" for rank I, "Warrior_e3" for rank III — what the army counts and saves.
    public static class Troops
    {
        private const string RANK = "_e";
        private static readonly string[] ROMAN = { "I", "II", "III", "IV", "V", "VI" };

        public static string Of(string unit, int rank) => rank <= 1 ? unit : unit + RANK + rank;

        public static string UnitOf(string troop)
        {
            var at = troop.IndexOf(RANK, StringComparison.Ordinal);
            return at < 0 ? troop : troop.Substring(0, at);
        }

        public static int RankOf(string troop)
        {
            var at = troop.IndexOf(RANK, StringComparison.Ordinal);
            return at < 0 ? 1 : int.Parse(troop.Substring(at + RANK.Length));
        }

        public static string Roman(int rank) => rank >= 1 && rank <= ROMAN.Length ? ROMAN[rank - 1] : rank.ToString();
    }
}
