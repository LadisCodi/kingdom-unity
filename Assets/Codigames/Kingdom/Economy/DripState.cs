namespace Codigames.Kingdom.Economy
{
    public class DripState
    {
        // Epoch milliseconds: when the unit being gained started; null while it is full.
        public double? AccruingSince { get; set; }
    }
}
