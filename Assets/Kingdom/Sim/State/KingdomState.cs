using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // What outlives the city: builders, the kingdom purse, Knowledge, the Survey, the profile.
    public sealed class KingdomState
    {
        public double Builders { get; set; }
        public Dictionary<string, double> Wallet { get; set; }
        public double LastKnowledgeAt { get; set; }
        public double KnowledgeBoughtWithGold { get; set; }
        public double UtcOffsetMinutes { get; set; }
        public SurveyState Survey { get; set; }
        public ProfileState Profile { get; set; }
        public TradeState Trade { get; set; }
    }
}
