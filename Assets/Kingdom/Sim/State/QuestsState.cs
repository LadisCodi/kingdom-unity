namespace Kingdom.Sim.State
{
    // Where the quest chain is.
    public sealed class QuestsState
    {
        public double Index { get; set; }
        public double Progress { get; set; }
        public QuestRush Rush { get; set; }
    }
}
