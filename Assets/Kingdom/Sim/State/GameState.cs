using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // The whole kingdom the sim advances: everything a save holds. Same shape as the web prototype's.
    public sealed class GameState
    {
        public string RegionId { get; set; }
        public City City { get; set; }
        public KingdomState Kingdom { get; set; }
        public PlayerState Player { get; set; }
        public FogState Fog { get; set; }
        // Cell key → the feature on that cell now.
        public Dictionary<string, string> Features { get; set; }
        public Dictionary<string, FeatureMeta> FeatureMeta { get; set; }
        public List<FeatureRespawn> FeatureRespawns { get; set; }
        // Cell key → its depot and exhaustion.
        public Dictionary<string, CellHarvestState> Harvest { get; set; }
        public List<Worker> Workers { get; set; }
        public List<ArmyUnit> Army { get; set; }
        public ResearchState Research { get; set; }
        public List<ScheduledEntry> Schedule { get; set; }
        public HeroesState Heroes { get; set; }
        public GachaState Gacha { get; set; }
        public AdsState Ads { get; set; }
        public LandmarksState Landmarks { get; set; }
        public Dictionary<string, LairState> Lairs { get; set; }
        public ArtifactsState Artifacts { get; set; }
        public List<Modifier> Modifiers { get; set; }
        public QuestsState Quests { get; set; }
        public Dictionary<string, double> Tallies { get; set; }
        public bool Replaying { get; set; }
        public Dictionary<string, bool> Discoveries { get; set; }
        public TutorialState Tutorial { get; set; }
        public AbandonedState Abandoned { get; set; }
        public BagState Bag { get; set; }
        public RelicsState Relics { get; set; }
        public SignalsState Signals { get; set; }
        public WorldState World { get; set; }
        public List<News> Notices { get; set; }
        public List<string> PendingDiscoveries { get; set; }
        public List<SimTrack> PendingAnalytics { get; set; }
        public double Seed { get; set; }
        // Monotonic counter for unique ids.
        public double NextId { get; set; }
        // Epoch ms: where the offline replay left off.
        public double LastAdvance { get; set; }
        public Dictionary<string, double> TapCarry { get; set; }
    }
}
