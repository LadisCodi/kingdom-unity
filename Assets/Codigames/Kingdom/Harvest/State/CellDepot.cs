namespace Codigames.Kingdom.Harvest.State
{
    // A cell that has been drawn on: what it still holds, and when it comes back full if it ran dry. A cell
    // never drawn on keeps no depot: it is full.
    public class CellDepot
    {
        public int Units { get; set; }

        // Epoch milliseconds; null while it still holds something.
        public double? ExhaustedUntil { get; set; }

        // How long the wait to full is, all told, in milliseconds: how far through it a cell is reads off it.
        public double WaitMs { get; set; }

        // Planted or moved, and not grown yet: its wait is its growth, and it comes back full.
        public bool Growing { get; set; }
    }
}
