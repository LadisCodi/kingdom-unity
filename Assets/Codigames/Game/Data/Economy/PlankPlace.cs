namespace Codigames.Game.Data.Economy
{
    // Where a currency reads on the header's plank.
    public enum PlankPlace
    {
        // Not on the plank: it reads in the one screen that spends it.
        Hidden,
        // A coin on the left, always.
        Coin,
        // A coin on the left once the kingdom holds some.
        CoinWhenHeld,
        // Past the rope on the right, always.
        Right,
    }
}
