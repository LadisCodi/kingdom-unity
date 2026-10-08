namespace Kingdom.Sim.State
{
    // A number moved for a while (or for good), globally or in a zone.
    public sealed class Modifier
    {
        // Deterministic and persisted; the fold order.
        public string Id { get; set; }
        public string Source { get; set; }
        public string Stat { get; set; }
        // A currency, harvest source or district; null = everything.
        public string Scope { get; set; }
        // add | mul.
        public string Op { get; set; }
        public double Value { get; set; }
        // Half-open: active while t < expiresAt. Null = permanent.
        public double? ExpiresAt { get; set; }
        // A zone; absent on global modifiers.
        public ModifierArea Area { get; set; }
    }
}
