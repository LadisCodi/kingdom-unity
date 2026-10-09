namespace Codigames.Kingdom.Research
{
    // What the researched technologies do to a number, read where that number is computed:
    // (base + flat) × (1 + percent). With nothing researched it is the exact identity.
    public interface IBonuses
    {
        // The unaimed effects plus the ones aimed at exactly this subject.
        BonusTotals Totals(string stat, TargetKind target = TargetKind.Global, string targetId = null);
    }
}
