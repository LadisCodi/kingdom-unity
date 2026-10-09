using System.Linq;
using Codigames.Game.UI.Research;
using NUnit.Framework;

namespace Codigames.Game.Tests
{
    // The research page is drawn from the web's geometry: three 120-pixel columns, connectors in the gutters.
    public class TechPageLayoutTests
    {
        [Test]
        public void PageWidth_ShouldHoldThreeColumnsAndTwoChannels()
        {
            Assert.That(TechPageLayout.PageWidth, Is.EqualTo(400));
            Assert.That(TechPageLayout.ColumnCentre(1), Is.EqualTo(200));
        }

        [Test]
        public void AStraightConnector_ShouldBeARunAndAHead()
        {
            var path = TechPageLayout.EdgePath(0, 1, 144, 1, true);
            var pieces = TechPageLayout.Pieces(path);

            Assert.That(pieces.Select(p => p.Kind), Is.EqualTo(new[] { EdgePieceKind.Vertical, EdgePieceKind.Head }));
            Assert.That(pieces[0].Length, Is.EqualTo(144 - TechPageLayout.NODE_H));
        }

        [Test]
        public void ABentConnector_ShouldTurnInTheGutterAboveItsTarget()
        {
            var path = TechPageLayout.EdgePath(0, 0, 144, 2, true);
            var pieces = TechPageLayout.Pieces(path);

            Assert.That(path[1].Y, Is.EqualTo(144 - TechPageLayout.ROW_GAP / 2));
            Assert.That(pieces.Where(p => p.Kind == EdgePieceKind.Elbow).Select(p => p.Turn),
                Is.EqualTo(new[] { ElbowTurn.TopRight, ElbowTurn.BottomLeft }));
        }

        [Test]
        public void ABlockedColumn_ShouldSendTheConnectorDownTheSideChannel()
        {
            var path = TechPageLayout.EdgePath(0, 0, 288, 0, false);

            Assert.That(path.Any(p => p.X == TechPageLayout.CHANNEL_W / 2), Is.True);
        }
    }
}
