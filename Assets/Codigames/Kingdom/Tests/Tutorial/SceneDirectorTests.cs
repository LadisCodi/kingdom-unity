using Codigames.Kingdom.Tutorial;
using Codigames.Kingdom.Tutorial.State;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Tutorial
{
    public class SceneDirectorTests
    {
        private FakeConditions _conditions;
        private FakePurse _purse;
        private FakeMorning _morning;
        private FakeContext _context;
        private TutorialState _state;

        [SetUp]
        public void SetUp()
        {
            _conditions = new FakeConditions();
            _purse = new FakePurse();
            _morning = new FakeMorning();
            _context = new FakeContext();
            _state = new TutorialState();
        }

        private SceneDirector Director(params Scene[] scenes) => new(Scenes.Of(scenes), _state, _conditions, _purse, _morning);

        [Test]
        public void TheFirstTriggeredSceneNotYetPlayedStarts()
        {
            var director = Director(new Scene { Id = "a", Trigger = new Condition(ConditionKind.QuestReached, "Q1") }, new Scene { Id = "b" });

            Assert.That(director.Pick(_context, false).Scene.Id, Is.EqualTo("b"));

            _conditions.True.Add((ConditionKind.QuestReached, "Q1"));
            Assert.That(director.Pick(_context, false).Scene.Id, Is.EqualTo("a"));

            director.MarkPlayed(director.Pick(_context, false).Scene);
            Assert.That(director.Pick(_context, false).Scene.Id, Is.EqualTo("b"));
        }

        [Test]
        public void AVeteranIsTaughtNothing()
        {
            _state.Veteran = true;
            Assert.That(Director(new Scene { Id = "a" }).Pick(_context, false).Scene, Is.Null);
        }

        [Test]
        public void ASceneAlreadyDoneIsSettledUnplayed()
        {
            _conditions.True.Add((ConditionKind.TechDone, "Mining"));
            var director = Director(new Scene { Id = "a", DoneWhen = new Condition(ConditionKind.TechDone, "Mining") }, new Scene { Id = "b" });

            var pick = director.Pick(_context, false);

            Assert.That(pick.Scene.Id, Is.EqualTo("b"));
            Assert.That(pick.Settled, Has.Count.EqualTo(1));
            Assert.That(pick.Settled[0].Id, Is.EqualTo("a"));
        }

        [Test]
        public void AnIntroductionWaitsOutTheBreathButAMorningBeatDoesNot()
        {
            var intro = Director(new Scene { Id = "a" });
            var beat = Director(new Scene { Id = "b", Skippable = false });

            Assert.That(intro.Pick(_context, true).Scene, Is.Null);
            Assert.That(beat.Pick(_context, true).Scene.Id, Is.EqualTo("b"));
        }

        [Test]
        public void AnIntroductionThatCannotStartHereLetsTheNextGoFirst()
        {
            var director = Director(new Scene { Id = "world", Where = SceneWhere.World }, new Scene { Id = "city" });

            Assert.That(director.Pick(_context, false).Scene.Id, Is.EqualTo("city"));
        }

        [Test]
        public void AMorningBeatThatCannotStartHereHoldsTheOthers()
        {
            var director = Director(new Scene { Id = "beat", Skippable = false, Where = SceneWhere.World }, new Scene { Id = "city" });

            Assert.That(director.Pick(_context, false).Scene, Is.Null);
        }

        [Test]
        public void ASheetHoldsBackAllButTheScenesThatPlayAnywhere()
        {
            _context.HasOpenSheet = true;

            Assert.That(Director(new Scene { Id = "a" }).Pick(_context, false).Scene, Is.Null);
            Assert.That(Director(new Scene { Id = "b", Anywhere = true }).Pick(_context, false).Scene.Id, Is.EqualTo("b"));
        }

        [Test]
        public void SomethingHeldBackHoldsEveryScene()
        {
            _context.HeldBack = true;
            Assert.That(Director(new Scene { Id = "a", Skippable = false }).Pick(_context, false).Scene, Is.Null);
        }

        [Test]
        public void ALessonThePurseCannotPayForWaits()
        {
            _purse.Unaffordable.Add("a");
            var director = Director(new Scene { Id = "a" }, new Scene { Id = "b" });

            Assert.That(director.Pick(_context, false).Scene.Id, Is.EqualTo("b"));
        }

        [Test]
        public void WhatStandsPastTheFogWaitsForTheFirstMorningToEnd()
        {
            _morning.FirstMorningOn = true;
            _conditions.True.Add((ConditionKind.Sighted, ""));
            var director = Director(new Scene { Id = "a", Trigger = new Condition(ConditionKind.Sighted) });

            Assert.That(director.Pick(_context, false).Scene, Is.Null);
            _morning.FirstMorningOn = false;
            Assert.That(director.Pick(_context, false).Scene.Id, Is.EqualTo("a"));
        }
    }
}
