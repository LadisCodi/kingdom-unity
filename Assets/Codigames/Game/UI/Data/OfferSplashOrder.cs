namespace Codigames.Game.UI.Data
{
    // An offer's splash asked for: which product, and whether the session raised it by itself.
    public sealed class OfferSplashOrder
    {
        public string Sku;
        public bool Auto;
        // Opened from the map's widget: every widget offer in a row along its top, to turn to.
        public bool Browse;
    }
}
