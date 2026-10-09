using System.Collections.Generic;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Research.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Tests.Builders
{
    // A city with a tree on it: the bar drips a point an hour up to 10, Knowledge on hand when a test says so,
    // and the province as revealed as a test sets it. Band 2 of the Kingdom opens on 20 cells and pays 3.
    public class ResearchFixture : CityFixture
    {
        public ResearchFixture(params ITechnology[] technologies) : base(technologies)
        {
            Bar = new KnowledgeBar(Knowledge, Treasury, KnowledgeSettings);
            Market = new KnowledgeMarket(Knowledge, Treasury, KnowledgeSettings);
            Research = new Researching(State, Technologies, Tree, Shelf, Explored, Bar, Treasury);
            Timeline.Register(Bar);
        }

        public ResearchState State => ResearchState;
        public KnowledgeState Knowledge { get; } = new();
        public FakeKnowledgeSettings KnowledgeSettings { get; } = new();
        public FakeTree Tree { get; } = new();
        public FakeShelf Shelf { get; } = new();
        public FakeExplored Explored { get; } = new();
        public KnowledgeBar Bar { get; }
        public KnowledgeMarket Market { get; }
        public Researching Research { get; }

        public void GiveKnowledge(double amount) => Treasury.Add(KnowledgeBar.KNOWLEDGE, amount);

        public sealed class FakeKnowledgeSettings : IKnowledgeSettings
        {
            public double PerHour => 1;
            public double Cap => 10;
            public double GoldPriceBase => 400;
            public double GoldPriceExponent => 2;
            public double GemsPerPoint => 200;
            public double LandmarkClaimLump => 3;
        }

        public sealed class FakeTree : ITechTree
        {
            public IReadOnlyList<string> Tomes => new[] { "Kingdom", "Sagas" };
            public int Eras(string tome) => tome == "Kingdom" ? 2 : 1;
            public int CellsToOpen(string tome, int era) => era <= 1 ? 0 : 20;
            public int? EraReward(string tome, int era) => tome == "Kingdom" && era == 2 ? 3 : null;
        }

        public sealed class FakeShelf : IBookshelf
        {
            public HashSet<string> Open { get; } = new() { "Kingdom" };
            public bool IsOpen(string tome) => Open.Contains(tome);
            public bool IsFound(string tome) => IsOpen(tome);
        }

        public sealed class FakeExplored : IExploredGround
        {
            public int RevealedCount { get; set; }
        }
    }
}
