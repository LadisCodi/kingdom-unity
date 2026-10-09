namespace Codigames.Game.UI.Data
{
    // One currency of a price: which, the amount as the player reads it, and whether the kingdom is short of it.
    public sealed class PriceTerm
    {
        public PriceTerm(string currency, string amount, bool isShort)
        {
            Currency = currency;
            Amount = amount;
            IsShort = isShort;
        }

        public string Currency { get; }
        public string Amount { get; }
        public bool IsShort { get; }
    }
}
