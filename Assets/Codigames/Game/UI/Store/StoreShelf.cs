using System.Collections.Generic;

namespace Codigames.Game.UI.Store
{
    // One shelf of a store page: a ribbon, a grid of cards, wide rows.
    public sealed class StoreShelf
    {
        public string Ribbon;
        public int Columns = 2;
        public float CardHeight = 560;
        public List<(string Id, StoreCardData Data)> Cards = new();
        public List<(string Id, StoreWideData Data)> Rows = new();
        public List<(string Id, StoreOfferData Data)> Offers = new();
        public List<(string Id, StoreDailyData Data)> Dailies = new();
        // A countdown on the ribbon (today's draw ends).
        public double? RibbonUntil;
    }
}
