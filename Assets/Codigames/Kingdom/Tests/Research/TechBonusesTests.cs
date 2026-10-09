using Codigames.Kingdom.Research;
using Codigames.Kingdom.Tests.Builders;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Research
{
    public class TechBonusesTests
    {
        private static ResearchFixture Tree() => new(
            new TechBuilder("Sawpits I").Moves("harvestYield", EffectOp.Percent, 5).Build(),
            new TechBuilder("Sawpits II").Moves("harvestYield", EffectOp.Percent, 5).Build(),
            new TechBuilder("Lumberjacks").Moves("harvestYield", EffectOp.Percent, 10, TargetKind.Harvest, "Forest").Build(),
            new TechBuilder("Bunks").Moves("populationCapacity", EffectOp.Flat, 2, TargetKind.District, "Housing").Build(),
            new TechBuilder("Forestry").Opens(UnlockKind.Harvest, "Forest").Opens(UnlockKind.DistrictLevel, "Townhall", 2).Build(),
            new TechBuilder("Draft").Unplaced().Opens(UnlockKind.District, "Mine").Build());

        [Test]
        public void NothingResearched_ShouldBeTheIdentity()
        {
            var fixture = Tree();

            Assert.That(fixture.Bonuses.Apply("harvestYield", 7.3), Is.EqualTo(7.3));
            Assert.That(fixture.Bonuses.Apply("populationCapacity", 4, TargetKind.District, "Housing"), Is.EqualTo(4));
        }

        [Test]
        public void Totals_ShouldAddPerEffectAndAimExactly()
        {
            var fixture = Tree();
            fixture.State.Completed.AddRange(new[] { "Sawpits I", "Sawpits II", "Lumberjacks", "Bunks" });

            Assert.That(fixture.Bonuses.Multiplier("harvestYield"), Is.EqualTo(1 + (5.0 / 100 + 5.0 / 100)));
            Assert.That(fixture.Bonuses.Multiplier("harvestYield", TargetKind.Harvest, "Forest"),
                Is.EqualTo(1 + (5.0 / 100 + 5.0 / 100 + 10.0 / 100)));
            Assert.That(fixture.Bonuses.Multiplier("harvestYield", TargetKind.Harvest, "Stone"), Is.EqualTo(1.1).Within(1e-12));
            Assert.That(fixture.Bonuses.Apply("populationCapacity", 4, TargetKind.District, "Housing"), Is.EqualTo(6));
            Assert.That(fixture.Bonuses.Apply("populationCapacity", 4), Is.EqualTo(4), "an aimed bonus never reaches an unaimed query");
        }

        [Test]
        public void Totals_ShouldFollowNewResearch()
        {
            var fixture = Tree();
            Assert.That(fixture.Bonuses.Multiplier("harvestYield"), Is.EqualTo(1));

            fixture.State.Completed.Add("Sawpits I");

            Assert.That(fixture.Bonuses.Multiplier("harvestYield"), Is.EqualTo(1.05));
        }

        [Test]
        public void Gates_ShouldBeWhatTheTechnologiesSayTheyOpen()
        {
            var fixture = Tree();

            Assert.That(fixture.Gates.HarvestTech("Forest"), Is.EqualTo("Forestry"));
            Assert.That(fixture.Gates.LevelTech("Townhall", 2), Is.EqualTo("Forestry"));
            Assert.That(fixture.Gates.LevelTech("Townhall", 3), Is.Null);
            Assert.That(fixture.Gates.DistrictTech("Mine"), Is.Null, "a card off the page opens nothing");

            Assert.That(fixture.Gates.IsOpen("Forestry"), Is.False);
            fixture.State.Completed.Add("Forestry");
            Assert.That(fixture.Gates.IsOpen("Forestry"), Is.True);
            Assert.That(fixture.Gates.IsOpen(null), Is.True);
        }
    }
}
