namespace Codigames.Kingdom.Research
{
    // One number a bonus moves: which (its stat), how, by how much (always positive: every bonus climbs) and at
    // what. A percent is authored in points: 10 is +10%.
    public readonly struct TechEffect
    {
        public TechEffect(string stat, EffectOp op, double value, TargetKind target = TargetKind.Global, string targetId = null)
        {
            Stat = stat;
            Op = op;
            Value = value;
            Target = target;
            TargetId = targetId;
        }

        public string Stat { get; }
        public EffectOp Op { get; }
        public double Value { get; }
        public TargetKind Target { get; }

        // The district, unit, tag, source or book it aims at; null when it is global.
        public string TargetId { get; }
    }
}
