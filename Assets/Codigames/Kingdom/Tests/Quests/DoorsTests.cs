using System.Collections.Generic;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Doors;
using Codigames.Kingdom.Quests;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Sites.State;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Kingdom.Tutorial.State;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Quests
{
    public class DoorsTests
    {
        private sealed class Position : IChainPosition
        {
            public int Index { get; set; }
        }

        private sealed class Quest : IQuestDefinition
        {
            public string Id { get; set; }
            public string Name => Id;
            public GoalType GoalType => GoalType.CollectTaps;
            public string GoalTarget => null;
            public double GoalAmount => 1;
            public int GoalLevel => 0;
            public IReadOnlyDictionary<string, double> Reward => new Dictionary<string, double>();
            public double RewardKnowledge => 0;
            public IReadOnlyDictionary<string, double> RewardItems => new Dictionary<string, double>();
            public bool AutoClaim => false;
            public double TutorialRentSeconds => 0;
        }

        private sealed class Ruin : IAbandonedSite
        {
            public string Id => "OldHouse";
            public string District => "Housing";
            public Vector2Int Anchor => new(4, -1);
            public int Sight => 3;
            public string Name => "The Millers' house";
        }

        private sealed class Sites : IProvinceSites
        {
            public IReadOnlyList<IAbandonedSite> Abandoned => new IAbandonedSite[] { new Ruin() };
            public IReadOnlyList<ILandmarkSite> Landmarks => new ILandmarkSite[0];
        }

        private sealed class Fixture : ResearchFixture
        {
            public Fixture()
            {
                var quests = new Catalog<IQuestDefinition>(new IQuestDefinition[]
                {
                    new Quest { Id = "FirstSteps" }, new Quest { Id = "Woodcraft" }, new Quest { Id = "TaxDay" }, new Quest { Id = "GrowingTown" },
                });
                Doors = new Doors.Doors(Tutorial, Chain, quests, Research, City, SitesState, new Sites(), Settings);
                Openings = new Openings(Doors, Shelf, Tree, Tutorial);
                Openings.Opened += (doors, books) =>
                {
                    OpenedDoors.AddRange(doors);
                    OpenedBooks.AddRange(books);
                };
            }

            public Openings Openings { get; }
            public List<DoorId> OpenedDoors { get; } = new();
            public List<string> OpenedBooks { get; } = new();

            public TutorialState Tutorial { get; } = new();
            public Position Chain { get; } = new();
            public SitesState SitesState { get; } = new();
            public Doors.Doors Doors { get; }
        }

        [Test]
        public void Research_ShouldOpenWhenTheChainReachesItsQuest()
        {
            var fixture = new Fixture();
            Assert.That(fixture.Doors.IsOpen(DoorId.Research), Is.False);

            fixture.Chain.Index = 1;

            Assert.That(fixture.Doors.IsOpen(DoorId.Research), Is.True);
            Assert.That(fixture.Doors.FreshlyOpen(), Is.EquivalentTo(new[] { DoorId.Research, DoorId.Knowledge }));
        }

        [Test]
        public void Build_ShouldNotOpenForARepairedRuinButForTheKingdomsOwn()
        {
            var fixture = new Fixture();
            fixture.SitesState.Repaired.Add("OldHouse");
            fixture.City.Districts.Add(new DistrictState { Id = "a", DefinitionId = "Housing", Anchor = new Vector2Int(4, -1), Built = true });
            Assert.That(fixture.Doors.IsOpen(DoorId.Build), Is.False);

            fixture.City.Districts.Add(new DistrictState { Id = "b", DefinitionId = "Housing", Anchor = new Vector2Int(-3, 3), Built = false });

            Assert.That(fixture.Doors.IsOpen(DoorId.Build), Is.True);
        }

        [Test]
        public void ADoorSeen_ShouldStayOpen()
        {
            var fixture = new Fixture();
            fixture.Chain.Index = 1;
            fixture.Doors.MarkSeen(DoorId.Research);
            fixture.Chain.Index = 0;

            Assert.That(fixture.Doors.IsOpen(DoorId.Research), Is.True);
            Assert.That(fixture.Doors.FirstMorningOn, Is.True);
        }

        [Test]
        public void AnOpening_ShouldBeAnnouncedOnceAndRemembered()
        {
            var fixture = new Fixture();
            fixture.Chain.Index = 1;

            fixture.Openings.Take();
            fixture.Openings.Take();

            Assert.That(fixture.OpenedDoors, Is.EqualTo(new[] { DoorId.Research, DoorId.Knowledge }));
            fixture.Chain.Index = 0;
            Assert.That(fixture.Doors.IsOpen(DoorId.Research), Is.True, "a door once open stays open");
        }

        [Test]
        public void AFoundBook_ShouldBeAnnouncedOnce_ButNeverTheKingdomsOwn()
        {
            var fixture = new Fixture();
            fixture.Openings.Take();
            Assert.That(fixture.OpenedBooks, Is.Empty);

            fixture.Shelf.Open.Add("Sagas");
            fixture.Openings.Take();
            fixture.Openings.Take();

            Assert.That(fixture.OpenedBooks, Is.EqualTo(new[] { "Sagas" }));
        }

        [Test]
        public void AVeteran_ShouldHaveNothingAnnounced()
        {
            var fixture = new Fixture();
            fixture.Tutorial.Veteran = true;
            fixture.Chain.Index = 1;
            fixture.Shelf.Open.Add("Sagas");

            fixture.Openings.Take();

            Assert.That(fixture.OpenedDoors, Is.Empty);
            Assert.That(fixture.OpenedBooks, Is.Empty);
        }
    }
}
