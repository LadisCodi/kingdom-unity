using Codigames.Kingdom.Economy;

namespace Codigames.Kingdom.Research.State
{
    public class KnowledgeState : DripState
    {
        // Points ever bought with Gold: what the next one costs. It never resets.
        public int BoughtWithGold { get; set; }
    }
}
