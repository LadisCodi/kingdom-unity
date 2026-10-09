namespace Codigames.Kingdom.Quests.State
{
    public class QuestState
    {
        // The active quest's place in the chain; past the end, the chain is done.
        public int Index { get; set; }

        // What a relative goal has counted since its quest became active.
        public double Progress { get; set; }

        // The quest whose rent rush is stamped, and when it lands; null once it has.
        public int RushIndex { get; set; } = -1;
        public double? RushAt { get; set; }
    }
}
