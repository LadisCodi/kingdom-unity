using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Fog.State;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Map;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Map
{
    public class FootprintsTests
    {
        private sealed class Mountain : IFeatureDefinition
        {
            public string Id => "Mountain";
            public string Source => "Stone";
            public string RespawnTerrain => "Grassland";
            public int MaxFootprint => 3;
        }

        private static Vector2Int[] Square(int x, int y, int size)
            => Enumerable.Range(0, size * size).Select(i => new Vector2Int(x + i % size, y + i / size)).ToArray();

        [Test]
        public void Group_ShouldTakeTheLargestSquaresFirstRowByRow()
        {
            var cells = Square(0, 0, 3).Concat(Square(3, 0, 2)).Append(new Vector2Int(5, 0)).ToList();

            var blocks = Footprints.Group(cells, 3);

            Assert.That(blocks, Is.EquivalentTo(new[]
            {
                (new Vector2Int(0, 0), 3), (new Vector2Int(3, 0), 2), (new Vector2Int(5, 0), 1),
            }));
        }

        [Test]
        public void Group_ShouldGiveTheSameBlocksWhateverOrderTheCellsComeIn()
        {
            var cells = Square(0, 0, 2).Concat(Square(2, 1, 2)).ToList();

            Assert.That(Footprints.Group(cells.AsEnumerable().Reverse(), 3), Is.EqualTo(Footprints.Group(cells, 3)));
        }

        // A 2 × 2 mountain next to the Townhall's ground: one thing in the fog.
        private sealed class Fixture : CityFixture
        {
            public Fixture()
            {
                foreach (var cell in Square(3, 0, 2)) Map.Place(cell, "Mountain");
                Footprints = new Footprints(Map, new Catalog<IFeatureDefinition>(new IFeatureDefinition[] { new Mountain() }));
                Fog = new FogOfWar(new FogState(), City, Map, Buildings, Settings, new FogSettings(), Treasury, footprints: Footprints);
                Single = new FogOfWar(new FogState(), City, Map, Buildings, Settings, new FogSettings(), Treasury);
            }

            public Footprints Footprints { get; }
            public FogOfWar Fog { get; }
            public FogOfWar Single { get; }

            protected override IBuildingDefinition MakeTownhall()
                => new BuildingBuilder().WithId("Townhall").WithMaxLevel(5).WithSize(2, 2).NotBuildable().WithFog(1, 2).Build();
        }

        private sealed class FogSettings : IFogSettings
        {
            public System.Collections.Generic.IReadOnlyList<double> CostPerRing => new double[] { 4, 8, 20, 85 };
            public double FallbackGrowth => 2;
            public int TapsToReveal => 5;
            public double MinCost => 1;
            public int CountStep => 10;
            public double CountGrowth => 1.05;
            public System.Collections.Generic.IReadOnlyList<int> ReachPerTownhallLevel => new[] { 3, 5 };
        }

        [Test]
        public void ABlock_ShouldCostTheSumOfItsCellsAndClearWhole()
        {
            var fixture = new Fixture();
            var far = new Vector2Int(4, 1);

            Assert.That(fixture.Fog.Cost(far), Is.EqualTo(Square(3, 0, 2).Sum(fixture.Single.Cost)));
            Assert.That(fixture.Fog.IsPayable(far), Is.True, "one of its cells touches the cleared ground");

            for (var i = 0; i < 4; i++) fixture.Fog.Tap(i % 2 == 0 ? far : new Vector2Int(3, 0));
            Assert.That(fixture.Fog.TapsDone(new Vector2Int(4, 0)), Is.EqualTo(4), "a tap on any cell advances the block");
            Assert.That(fixture.Fog.Tap(far), Is.EqualTo(RevealResult.Revealed));

            Assert.That(Square(3, 0, 2).All(fixture.Fog.IsRevealed), Is.True);
        }
    }
}
