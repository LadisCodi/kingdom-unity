namespace Codigames.Kingdom.Harvest
{
    // What a tap on the ground did: what it paid, of which currency, or why it paid nothing.
    public readonly struct TapResult
    {
        public TapResult(TapRefusal refusal, string currency = null, double paid = 0, bool emptied = false, string requiredTech = null)
        {
            RequiredTech = requiredTech;
            Refusal = refusal;
            Currency = currency;
            Paid = paid;
            Emptied = emptied;
        }

        public TapRefusal Refusal { get; }
        public string Currency { get; }
        public double Paid { get; }

        // This tap took the last of what the cell held.
        public bool Emptied { get; }

        // NeedsResearch: the technology that opens the source.
        public string RequiredTech { get; }
    }
}
