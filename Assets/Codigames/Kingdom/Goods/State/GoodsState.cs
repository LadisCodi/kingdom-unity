using System.Collections.Generic;

namespace Codigames.Kingdom.Goods.State
{
    // The city's refined goods, by good — a counter, not a wallet row — and each workshop's queue.
    public class GoodsState
    {
        public Dictionary<string, int> Stock { get; set; } = new();

        public Dictionary<string, WorkshopLine> Lines { get; set; } = new();
    }

    // A workshop's queue, front first, and the moment its work was last counted.
    public class WorkshopLine
    {
        public List<WorkshopItem> Items { get; set; } = new();

        // Epoch milliseconds.
        public double Anchor { get; set; }
    }

    // One good on the bench: worker-milliseconds done, and needed — priced when it was queued.
    public class WorkshopItem
    {
        public string Good { get; set; }
        public double WorkMs { get; set; }
        public double NeedMs { get; set; }
    }
}
