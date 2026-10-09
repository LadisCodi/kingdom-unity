using System.Collections.Generic;
using Codigames.Kingdom.Bag.State;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Fog.State;
using Codigames.Kingdom.Harvest.State;
using Codigames.Kingdom.Magic.State;
using Codigames.Kingdom.Notices.State;
using Codigames.Kingdom.Quests.State;
using Codigames.Kingdom.Research.State;
using Codigames.Kingdom.Sites.State;
using Codigames.Kingdom.Tutorial.State;

namespace Codigames.Kingdom
{
    // Everything a kingdom is that changes: what a save holds.
    public class KingdomState
    {
        public CityState City { get; set; } = new();
        public GroundState Ground { get; set; } = new();

        public HarvestState Harvest { get; set; } = new();

        public ManaState Mana { get; set; } = new();

        public FogState Fog { get; set; } = new();

        public ResearchState Research { get; set; } = new();

        public KnowledgeState Knowledge { get; set; } = new();

        public SitesState Sites { get; set; } = new();

        public QuestState Quests { get; set; } = new();

        public TutorialState Tutorial { get; set; } = new();

        public BagState Bag { get; set; } = new();

        public NoticesState Notices { get; set; } = new();

        public Goods.State.GoodsState Goods { get; set; } = new();

        public Army.State.ArmyState Army { get; set; } = new();

        public Lairs.State.LairsState Lairs { get; set; } = new();

        public Heroes.State.HeroesState Heroes { get; set; } = new();

        public Heroes.State.GachaState Gacha { get; set; } = new();

        // This kingdom's own randomness: every draw hashes it with the parts that name the event.
        public uint Seed { get; set; }

        // Every currency's balance, whoever holds it.
        public Dictionary<string, double> Balances { get; set; } = new();

        // Epoch milliseconds: where the last advance left off.
        public double LastAdvance { get; set; }
    }
}
