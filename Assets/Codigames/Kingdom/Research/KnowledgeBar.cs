using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Research.State;

namespace Codigames.Kingdom.Research
{
    // What research is paid in: a bar that drips a point an hour up to its cap. Everything else — a quest, a
    // claim, a purchase — lands in full, over the cap if it must; the cost of a full bar is the drip it did not
    // earn.
    public class KnowledgeBar : Drip
    {
        public const string KNOWLEDGE = "Knowledge";

        private readonly IKnowledgeSettings _settings;

        public KnowledgeBar(KnowledgeState state, ITreasury treasury, IKnowledgeSettings settings)
            : base(state, treasury, KNOWLEDGE)
        {
            _settings = settings;
        }

        public override double Cap => _settings.Cap;

        public override double PerHour => _settings.PerHour;
    }
}
