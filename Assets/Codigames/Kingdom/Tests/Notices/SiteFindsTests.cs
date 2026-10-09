using System.Collections.Generic;
using Codigames.Kingdom.Notices;
using Codigames.Kingdom.Notices.State;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Sites.State;
using Codigames.Kingdom.Tests.Tutorial;
using Codigames.Kingdom.Tutorial;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Notices
{
    public class SiteFindsTests
    {
        private sealed class Landmark : ILandmarkSite
        {
            public string Id { get; set; }
            public string Kind { get; set; } = "StandingStones";
            public Vector2Int Anchor { get; set; }
            public int Size { get; set; } = 1;
            public double ClaimCost { get; set; }
        }

        private sealed class Ruin : IAbandonedSite
        {
            public string Id { get; set; }
            public string District { get; set; } = "Housing";
            public Vector2Int Anchor { get; set; }
            public int Sight { get; set; }
            public string Name { get; set; } = "The old house";
        }

        private sealed class Sites : IProvinceSites
        {
            public IReadOnlyList<IAbandonedSite> Abandoned { get; set; }
            public IReadOnlyList<ILandmarkSite> Landmarks { get; set; }
        }

        private static readonly Vector2Int STONES = new(4, 0);
        private static readonly Vector2Int HOUSE = new(-4, 0);

        private readonly HashSet<Vector2Int> _inView = new();
        private SitesState _sites;
        private SiteFinds _finds;

        [SetUp]
        public void SetUp()
        {
            _inView.Clear();
            _sites = new SitesState();
            _finds = new SiteFinds(new NoticesState(), new Sites
            {
                Landmarks = new ILandmarkSite[] { new Landmark { Id = "Stones", Anchor = STONES } },
                Abandoned = new IAbandonedSite[] { new Ruin { Id = "OldHouse", Anchor = HOUSE } },
            }, _sites);
        }

        [Test]
        public void Sweep_ShouldFindASite_OnceItsCellIsInView()
        {
            Assert.That(_finds.Sweep(_inView.Contains), Is.Empty);

            _inView.Add(STONES);

            Assert.That(_finds.Sweep(_inView.Contains), Is.EqualTo(new[] { "Stones" }));
        }

        [Test]
        public void Sweep_ShouldFindASiteOnce()
        {
            _inView.Add(STONES);
            _finds.Sweep(_inView.Contains);

            Assert.That(_finds.Sweep(_inView.Contains), Is.Empty);
        }

        [Test]
        public void Sweep_ShouldPassOverARepairedRuin()
        {
            _sites.Repaired.Add("OldHouse");
            _inView.Add(HOUSE);

            Assert.That(_finds.Sweep(_inView.Contains), Is.Empty);
        }

        [Test]
        public void SceneIntroduces_ShouldBeTrue_ForASceneItTriggersOrALineThatPointsAtIt()
        {
            var scenes = new ISceneDefinition[]
            {
                new Scene { Id = "stones", Trigger = new Condition(ConditionKind.LandmarkSeen, "Stones") },
                new Scene { Id = "house", Lines = new ISceneLine[] { new Line { Point = "abandoned:OldHouse" } } },
            };

            Assert.That(NewsDesk.SceneIntroduces(scenes, "Stones"), Is.True);
            Assert.That(NewsDesk.SceneIntroduces(scenes, "OldHouse"), Is.True);
            Assert.That(NewsDesk.SceneIntroduces(scenes, "Leyspring"), Is.False);
        }
    }
}
