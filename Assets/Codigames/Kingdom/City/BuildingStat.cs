namespace Codigames.Kingdom.City
{
    // One number a building is worth at a level. `Currency` names the coin a store keeps; `OnCard` is false for a
    // figure only the upgrade sheet shows; `LowerIsBetter` for a wait.
    public sealed class BuildingStat
    {
        public BuildingStat(StatKind kind, double value, bool onCard = true, bool lowerIsBetter = false, string currency = null)
        {
            Kind = kind;
            Value = value;
            OnCard = onCard;
            LowerIsBetter = lowerIsBetter;
            Currency = currency;
        }

        public StatKind Kind { get; }
        public double Value { get; }
        public bool OnCard { get; }
        public bool LowerIsBetter { get; }
        public string Currency { get; }
    }
}
