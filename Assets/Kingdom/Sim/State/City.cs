using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // The province's city: its purse, its people, its buildings and their queues.
    public sealed class City
    {
        public string Name { get; set; }
        public Dictionary<string, double> Wallet { get; set; }
        public Dictionary<string, double> Goods { get; set; }
        public double Population { get; set; }
        public List<District> Districts { get; set; }
        public List<QueueItem> Queue { get; set; }
        public List<TrainingItem> TrainingQueue { get; set; }
        public Dictionary<string, WorkshopLine> Workshops { get; set; }
        public Dictionary<string, double> Wounded { get; set; }
        public double LastManaAt { get; set; }
    }
}
