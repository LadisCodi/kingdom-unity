using Codigames.Kingdom.City;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Harvest
{
    public class TransplantingTests
    {
        private static readonly Vector2Int PLOT = new(4, 0);
        private static readonly Vector2Int BARE = new(4, 1);

        // A tree at (3, 3) and a crop plot at (4, 0); everything revealed.
        private sealed class Fixture : HarvestFixture
        {
            public Fixture()
            {
                Ground.Features[PLOT] = "Crops";
                Transplanting = new Transplanting(Ground, Harvesting, Placement, Revealed, Gates);
            }

            public Transplanting Transplanting { get; }
        }

        [Test]
        public void ACropPlot_ShouldMoveAndGrowAgainWhereItLands()
        {
            var fixture = new Fixture();

            var refusal = fixture.Transplanting.Move(PLOT, BARE, 1000);

            Assert.That(refusal, Is.EqualTo(TransplantRefusal.None));
            Assert.That(fixture.Ground.Features.ContainsKey(PLOT), Is.False, "the cell it left is bare");
            Assert.That(fixture.Ground.Features[BARE], Is.EqualTo("Crops"));
            Assert.That(fixture.Harvesting.IsGrowing(BARE, 1000), Is.True);
            Assert.That(fixture.Harvesting.Regrowth(BARE, 3500), Is.EqualTo(0.5).Within(1e-9), "its whole growth, from nothing");
        }

        [Test]
        public void ATree_ShouldWaitForTransplanting()
        {
            var fixture = new Fixture();
            Assert.That(fixture.Transplanting.PickUpRefusal(HarvestFixture.TREE), Is.EqualTo(TransplantRefusal.NeedsResearch));

            fixture.ResearchState.Completed.Add(Transplanting.TRANSPLANTING);

            Assert.That(fixture.Transplanting.Move(HarvestFixture.TREE, BARE, 0), Is.EqualTo(TransplantRefusal.None));
            Assert.That(fixture.Ground.Features[BARE], Is.EqualTo("Trees"));
        }

        [Test]
        public void NothingElse_ShouldMove()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Transplanting.PickUpRefusal(HarvestFixture.BUSH), Is.EqualTo(TransplantRefusal.NotMovable));
            Assert.That(fixture.Transplanting.PickUpRefusal(HarvestFixture.ROCK), Is.EqualTo(TransplantRefusal.NotMovable));
            Assert.That(fixture.Transplanting.PickUpRefusal(BARE), Is.EqualTo(TransplantRefusal.NotMovable));
        }

        [Test]
        public void ItShouldNotLandOnSomethingElse_NorInTheFog()
        {
            var fixture = new Fixture();
            fixture.Revealed.Fogged.Add(BARE);

            Assert.That(fixture.Transplanting.Move(PLOT, HarvestFixture.BUSH, 0), Is.EqualTo(TransplantRefusal.Placement));
            Assert.That(fixture.Transplanting.LandingProblem(PLOT, BARE), Is.EqualTo(PlacementProblem.InFog));
            Assert.That(fixture.Ground.Features[PLOT], Is.EqualTo("Crops"), "a refused move leaves it standing");
        }

        [Test]
        public void PutBackWhereItStarted_ShouldBeACancel()
        {
            var fixture = new Fixture();
            fixture.Harvesting.Tap(PLOT, 0);
            var held = fixture.Harvesting.UnitsAt(PLOT);

            Assert.That(fixture.Transplanting.Move(PLOT, PLOT, 0), Is.EqualTo(TransplantRefusal.None));
            Assert.That(fixture.Harvesting.UnitsAt(PLOT), Is.EqualTo(held), "nothing restarts");
            Assert.That(fixture.Harvesting.IsGrowing(PLOT, 0), Is.False);
        }
    }
}
