namespace Codigames.Kingdom.Magic.State
{
    public class ManaState
    {
        // Epoch milliseconds: when the unit the pool is gaining started; null while the pool is full.
        public double? AccruingSince { get; set; }
    }
}
