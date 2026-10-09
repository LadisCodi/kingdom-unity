using Codigames.Kingdom.Research;
using Codigames.Kingdom.Tests.Builders;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Research
{
    public class ResearchingTests
    {
        private static ResearchFixture Tree() => new(
            new TechBuilder("Forestry").Costs(2, 20).Build(),
            new TechBuilder("Farming").Requires("Forestry").Costs(3, 50).Build(),
            new TechBuilder("Masonry").In("Kingdom", 2, 1).Requires("Forestry").Costs(1, 10).Build(),
            new TechBuilder("Ballads").In("Sagas", 1).Costs(1, 10).Build(),
            new TechBuilder("Draft").Unplaced().Costs(1, 10).Build());

        [Test]
        public void Pour_ShouldTakeWhatTheBarHoldsUpToWhatIsMissing()
        {
            var fixture = Tree();
            fixture.GiveKnowledge(1);

            Assert.That(fixture.Research.Pour("Forestry", 0), Is.EqualTo(PourResult.Poured));
            Assert.That(fixture.Research.PouredInto("Forestry"), Is.EqualTo(1));
            Assert.That(fixture.Research.Pour("Forestry", 0), Is.EqualTo(PourResult.NothingHeld));

            fixture.GiveKnowledge(5);
            fixture.Research.Pour("Forestry", 0);

            Assert.That(fixture.Research.IsFilled("Forestry"), Is.True);
            Assert.That(fixture.Bar.Amount, Is.EqualTo(4));
            Assert.That(fixture.Research.Pour("Forestry", 0), Is.EqualTo(PourResult.AlreadyFull));
        }

        [Test]
        public void Pour_ShouldTakeAtMostWhatItIsAskedFor()
        {
            var fixture = Tree();
            fixture.GiveKnowledge(5);

            fixture.Research.Pour("Farming", 0, 1);

            Assert.That(fixture.Research.PouredInto("Farming"), Is.EqualTo(0), "Farming waits for Forestry");
            fixture.Research.Pour("Forestry", 0, 1);
            Assert.That(fixture.Research.PouredInto("Forestry"), Is.EqualTo(1));
        }

        [Test]
        public void Refusal_ShouldSayWhatHoldsATechnologyShut()
        {
            var fixture = Tree();

            Assert.That(fixture.Research.Refusal("Forestry"), Is.EqualTo(ResearchRefusal.None));
            Assert.That(fixture.Research.Refusal("Farming"), Is.EqualTo(ResearchRefusal.MissingRequirement));
            Assert.That(fixture.Research.Refusal("Ballads"), Is.EqualTo(ResearchRefusal.TomeClosed));
            Assert.That(fixture.Research.Refusal("Draft"), Is.EqualTo(ResearchRefusal.MissingRequirement));

            Complete(fixture, "Forestry");
            Assert.That(fixture.Research.Refusal("Masonry"), Is.EqualTo(ResearchRefusal.EraLocked));
            Assert.That(fixture.Research.EraShortfall("Kingdom", 2), Is.EqualTo(20));

            fixture.Explored.RevealedCount = 20;
            Assert.That(fixture.Research.Refusal("Masonry"), Is.EqualTo(ResearchRefusal.None));
            Assert.That(fixture.Research.Refusal("Forestry"), Is.EqualTo(ResearchRefusal.AlreadyDone));
        }

        [Test]
        public void Complete_ShouldWaitForTheKnowledgeThenPayTheGold()
        {
            var fixture = Tree();
            var gold = fixture.Treasury.Get("Gold");
            string researched = null;
            fixture.Research.Researched += id => researched = id;

            Assert.That(fixture.Research.Complete("Forestry"), Is.EqualTo(ResearchResult.NotFilled));

            fixture.GiveKnowledge(2);
            fixture.Research.Pour("Forestry", 0);
            Assert.That(fixture.Research.Complete("Forestry"), Is.EqualTo(ResearchResult.Researched));

            Assert.That(researched, Is.EqualTo("Forestry"));
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(gold - 20));
            Assert.That(fixture.Research.StateOf("Forestry"), Is.EqualTo(TechState.Done));
            Assert.That(fixture.Research.PouredInto("Forestry"), Is.EqualTo(0));
        }

        [Test]
        public void Complete_ShouldKeepThePouredKnowledgeWhenTheGoldIsShort()
        {
            var fixture = Tree();
            fixture.Treasury.TryPay(new System.Collections.Generic.Dictionary<string, double> { ["Gold"] = CityFixture.START_GOLD - 5 });
            fixture.GiveKnowledge(2);
            fixture.Research.Pour("Forestry", 0);

            Assert.That(fixture.Research.Complete("Forestry"), Is.EqualTo(ResearchResult.CannotAfford));
            Assert.That(fixture.Research.PouredInto("Forestry"), Is.EqualTo(2));
            Assert.That(fixture.Research.IsActionable("Forestry"), Is.False);
        }

        [Test]
        public void States_ShouldGoFromLockedThroughProgressToDone()
        {
            var fixture = Tree();

            Assert.That(fixture.Research.StateOf("Farming"), Is.EqualTo(TechState.Locked));
            Complete(fixture, "Forestry");
            Assert.That(fixture.Research.StateOf("Farming"), Is.EqualTo(TechState.Progress));
        }

        [Test]
        public void IsActionable_ShouldLightOnlyForAPressThatDoesSomething()
        {
            var fixture = Tree();

            Assert.That(fixture.Research.ActionableCount(), Is.EqualTo(0), "nothing in the bar");
            fixture.GiveKnowledge(1);
            Assert.That(fixture.Research.IsActionable("Forestry"), Is.False, "one point fills nothing that needs two");
            fixture.GiveKnowledge(1);
            Assert.That(fixture.Research.ActionableCount(), Is.EqualTo(1));
        }

        [Test]
        public void FinishingABand_ShouldPayItsRewardOnce()
        {
            var fixture = Tree();
            fixture.Explored.RevealedCount = 20;
            var paid = 0;
            fixture.Research.EraFinished += (tome, era, fragments) => paid += fragments;

            Complete(fixture, "Forestry");
            Complete(fixture, "Masonry");

            Assert.That(paid, Is.EqualTo(3));
            Assert.That(fixture.State.Rewarded, Is.EquivalentTo(new[] { "Kingdom:2" }));
        }

        [Test]
        public void Pour_ShouldStartTheDripAgainBelowTheCap()
        {
            var fixture = Tree();
            fixture.GiveKnowledge(10);
            Assert.That(fixture.Bar.NextUnitAt, Is.Null);

            fixture.Research.Pour("Forestry", 1000);

            Assert.That(fixture.Bar.NextUnitAt, Is.EqualTo(1000 + 3_600_000));
        }

        private static void Complete(ResearchFixture fixture, string id)
        {
            fixture.GiveKnowledge(fixture.Research.Missing(id));
            fixture.Research.Pour(id, 0);
            Assert.That(fixture.Research.Complete(id), Is.EqualTo(ResearchResult.Researched), id);
        }
    }
}
