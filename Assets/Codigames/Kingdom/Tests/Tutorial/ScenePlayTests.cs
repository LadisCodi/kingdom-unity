using System.Collections.Generic;
using Codigames.Kingdom.Tutorial;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Tutorial
{
    public class ScenePlayTests
    {
        private FakeConditions _conditions;
        private FakePurse _purse;

        [SetUp]
        public void SetUp()
        {
            _conditions = new FakeConditions();
            _purse = new FakePurse();
        }

        private ScenePlay Play(params Line[] lines) => new(new Scene { Id = "s", Lines = lines }, _conditions, _purse, () => 0);

        [Test]
        public void ATapLineWaitsForItsTap()
        {
            var play = Play(Line.Tap(), Line.Tap());
            play.Start();

            Assert.That(play.Index, Is.EqualTo(0));
            play.Next();
            Assert.That(play.Index, Is.EqualTo(1));
            play.Next();
            Assert.That(play.Finished, Is.True);
        }

        [Test]
        public void ALineAlreadyMetIsPassedWhenItBegins()
        {
            _conditions.True.Add((ConditionKind.Overlay, "build"));
            var play = Play(Line.Tap(), Line.Waiting(ConditionKind.Overlay, "build"), Line.Tap());
            play.Start();

            play.Next();

            Assert.That(play.Index, Is.EqualTo(2));
        }

        [Test]
        public void ASceneResumesAfterTheLastProgressAlreadyMade()
        {
            _conditions.True.Add((ConditionKind.QuestClaimed, "Q1"));
            var play = Play(Line.Tap(), Line.Waiting(ConditionKind.QuestClaimed, "Q1"), Line.Tap(), Line.Tap());

            play.Start();

            Assert.That(play.Index, Is.EqualTo(2));
        }

        [Test]
        public void AMomentIsNotProgress()
        {
            _conditions.True.Add((ConditionKind.Overlay, "build"));
            var play = Play(Line.Tap(), Line.Waiting(ConditionKind.Overlay, "build"));

            play.Start();

            Assert.That(play.Index, Is.EqualTo(0));
        }

        [Test]
        public void ALineThatStocksMakesUpThePurseWhenItEnds()
        {
            var stocked = new List<string>();
            var play = Play(new Line { Stocks = "Housing" }, Line.Tap());
            play.Stocked += (line, added) => stocked.Add(line.Stocks);
            play.Start();

            play.Next();

            Assert.That(stocked, Is.EqualTo(new[] { "Housing" }));
        }

        [Test]
        public void AStockingLineIsSkippedWhileThePurseCanPay()
        {
            _purse.Affordable.Add("Housing");
            var play = Play(new Line { Stocks = "Housing" }, Line.Tap());

            play.Start();

            Assert.That(play.Index, Is.EqualTo(1));
            Assert.That(_purse.StockedBuildings, Is.Empty);
        }

        [Test]
        public void ConditionNamesParseFromTheWeb()
        {
            Assert.That(Condition.Parse("questReached"), Is.EqualTo(ConditionKind.QuestReached));
            Assert.That(Condition.Parse("noOverlay"), Is.EqualTo(ConditionKind.NoOverlay));
            Assert.That(Condition.Parse(""), Is.EqualTo(ConditionKind.Unknown));
            Assert.That(Condition.Parse("somethingNew"), Is.EqualTo(ConditionKind.Unknown));
        }
    }
}
