using System.Linq;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Modules.Grid.Tests
{
    public class GridMathTests
    {
        [Test]
        public void Neighbours_ShouldBeTheFourSidesOnly()
        {
            var cells = GridMath.Neighbours(new Vector2Int(2, 2)).ToList();

            Assert.That(cells, Is.EquivalentTo(new[] { new Vector2Int(2, 1), new Vector2Int(3, 2), new Vector2Int(2, 3), new Vector2Int(1, 2) }));
            Assert.That(GridMath.AreNeighbours(new Vector2Int(0, 0), new Vector2Int(1, 1)), Is.False);
        }

        [Test]
        public void Distances_ShouldEachMeasureTheirOwnWay()
        {
            var a = new Vector2Int(0, 0);
            var b = new Vector2Int(3, 4);

            Assert.That(GridMath.Chebyshev(a, b), Is.EqualTo(4));
            Assert.That(GridMath.Manhattan(a, b), Is.EqualTo(7));
            Assert.That(GridMath.Euclidean(a, b), Is.EqualTo(5));
        }

        [Test]
        public void Rect_ShouldCoverWidthTimesHeightFromTheAnchor()
        {
            var cells = GridMath.Rect(new Vector2Int(1, 1), 2, 3).ToList();

            Assert.That(cells, Has.Count.EqualTo(6));
            Assert.That(cells.First(), Is.EqualTo(new Vector2Int(1, 1)));
            Assert.That(cells.Last(), Is.EqualTo(new Vector2Int(2, 3)));
        }

        [Test]
        public void ChebyshevToRect_ShouldBeZeroInsideAndCountRingsOutside()
        {
            var anchor = new Vector2Int(0, 0);

            Assert.That(GridMath.ChebyshevToRect(new Vector2Int(1, 1), anchor, 2, 2), Is.EqualTo(0));
            Assert.That(GridMath.ChebyshevToRect(new Vector2Int(3, 0), anchor, 2, 2), Is.EqualTo(2));
            Assert.That(GridMath.AroundRect(anchor, 2, 2, 1).Count(), Is.EqualTo(16));
        }
    }
}
