namespace Codigames.Kingdom.Harvest.State
{
    // A cell that has been drawn on: what it still holds, and when it comes back full if it ran dry. A cell
    // never drawn on keeps no depot: it is full.
    public class CellDepot
    {
        public int Units { get; set; }

        // Epoch milliseconds; null while it still holds something.
        public double? ExhaustedUntil { get; set; }
    }
}
