namespace Codigames.Kingdom.Research
{
    public static class BonusesExtensions
    {
        // The base, after the technologies.
        public static double Apply(this IBonuses bonuses, string stat, double value, TargetKind target = TargetKind.Global,
            string targetId = null)
            => bonuses == null ? value : bonuses.Totals(stat, target, targetId).Apply(value);

        // What the tree scales a number by: 1.15 for +15%.
        public static double Multiplier(this IBonuses bonuses, string stat, TargetKind target = TargetKind.Global,
            string targetId = null)
            => bonuses == null ? 1 : bonuses.Totals(stat, target, targetId).Multiplier;

        public static double Flat(this IBonuses bonuses, string stat, TargetKind target = TargetKind.Global, string targetId = null)
            => bonuses == null ? 0 : bonuses.Totals(stat, target, targetId).Flat;
    }
}
