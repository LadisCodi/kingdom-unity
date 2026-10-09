using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Research;

namespace Codigames.Game.UI.Research
{
    // A book's page, in page pixels (the web's layout.ts, so both builds draw one page): three columns of cards
    // read top to bottom, a chapter bar wherever the next band begins, and connectors that run in the gutters
    // and side channels so they never cross a card. The view scales the whole page to its width.
    public static class TechPageLayout
    {
        public const float NODE_W = 120;
        public const float NODE_H = 108;
        public const float COL_GAP = 6;
        public const float ROW_GAP = 36;
        public const float GATE_BAR_H = 56;
        public const float CHANNEL_W = 14;
        public const float ELBOW_R = 9;
        public const float EDGE_BAND = 8;

        private const float GATE_H = GATE_BAR_H + ROW_GAP;

        public static float PageWidth => TechTreeRules.COLUMNS * NODE_W + (TechTreeRules.COLUMNS - 1) * COL_GAP + 2 * CHANNEL_W;

        public static float ColumnLeft(int column) => CHANNEL_W + column * (NODE_W + COL_GAP);

        public static float ColumnCentre(int column) => ColumnLeft(column) + NODE_W / 2;

        // Every placed card of a book's page, by authored row, with a chapter bar before each band after the first.
        public static List<PageRow> Rows(IEnumerable<ITechnology> technologies, string tome)
        {
            var placed = technologies.Where(t => t.IsPlaced && t.Tome == tome && t.Column >= 0 && t.Column < TechTreeRules.COLUMNS).ToList();
            var rows = new List<PageRow>();
            var era = 0;
            foreach (var group in placed.GroupBy(t => t.Row).OrderBy(g => g.Key))
            {
                var rowEra = group.First().Era;
                for (var e = era + 1; e <= rowEra; e++)
                {
                    if (e > 1) rows.Add(PageRow.Gate(e));
                }

                era = Math.Max(era, rowEra);
                var slots = new string[TechTreeRules.COLUMNS];
                foreach (var tech in group) slots[tech.Column] = tech.Id;
                rows.Add(PageRow.Cards(group.Key, rowEra, slots));
            }

            foreach (var e in placed.Select(t => t.Era).Distinct().OrderBy(e => e))
            {
                if (e > era && e > 1) rows.Add(PageRow.Gate(e));
            }

            return rows;
        }

        // The top of each row, and the page's height.
        public static (List<float> Tops, float Height) RowTops(IReadOnlyList<PageRow> rows)
        {
            var tops = new List<float>();
            var y = 0f;
            foreach (var row in rows)
            {
                tops.Add(y);
                y += row.IsGate ? GATE_H : NODE_H + ROW_GAP;
            }

            return (tops, y);
        }

        // Out of the source's bottom, down its own column, across the gutter above the target and into its top
        // edge — or, when the column below the source is not clear, out into the side channel and back.
        public static List<(float X, float Y)> EdgePath(float fromTop, int fromColumn, float toTop, int toColumn, bool clear)
        {
            var startY = fromTop + NODE_H;
            var gutterBelow = startY + ROW_GAP / 2;
            var gutterAbove = toTop - ROW_GAP / 2;
            var x0 = ColumnCentre(fromColumn);
            var x1 = ColumnCentre(toColumn);
            if (clear)
            {
                if (Math.Abs(x0 - x1) < 0.01f) return new() { (x0, startY), (x1, toTop) };
                return new() { (x0, startY), (x0, gutterAbove), (x1, gutterAbove), (x1, toTop) };
            }

            var channelX = fromColumn + toColumn <= TechTreeRules.COLUMNS - 1 ? CHANNEL_W / 2 : PageWidth - CHANNEL_W / 2;
            return new() { (x0, startY), (x0, gutterBelow), (channelX, gutterBelow), (channelX, gutterAbove), (x1, gutterAbove), (x1, toTop) };
        }

        // A connector as the pieces it is drawn from: straight runs, elbows and the head at its end.
        public static List<EdgePiece> Pieces(IReadOnlyList<(float X, float Y)> points)
        {
            var pieces = new List<EdgePiece>();
            if (points.Count < 2) return pieces;

            for (var i = 0; i < points.Count - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var (dx, dy) = Direction(a, b);
                var trimA = i > 0 ? ELBOW_R : 0;
                var trimB = i + 1 < points.Count - 1 ? ELBOW_R : 0;
                var length = Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y) - trimA - trimB;
                if (length > 0)
                {
                    var sx = a.X + dx * trimA;
                    var sy = a.Y + dy * trimA;
                    pieces.Add(dx == 0
                        ? new EdgePiece(EdgePieceKind.Vertical, sx, dy > 0 ? sy : sy - length, length)
                        : new EdgePiece(EdgePieceKind.Horizontal, dx > 0 ? sx : sx - length, sy, length));
                }

                if (i + 1 < points.Count - 1)
                {
                    var turn = Turn(SideIn(dx, dy), SideOut(Direction(b, points[i + 2])));
                    if (turn != null) pieces.Add(new EdgePiece(EdgePieceKind.Elbow, b.X, b.Y, 0, turn.Value));
                }
            }

            var last = points[^1];
            pieces.Add(new EdgePiece(EdgePieceKind.Head, last.X, last.Y, 0));
            return pieces;
        }

        private static (int, int) Direction((float X, float Y) a, (float X, float Y) b) => (Math.Sign(b.X - a.X), Math.Sign(b.Y - a.Y));

        private static string SideIn(int dx, int dy) => dy > 0 ? "top" : dy < 0 ? "bottom" : dx > 0 ? "left" : "right";

        private static string SideOut((int X, int Y) d) => d.Y > 0 ? "bottom" : d.Y < 0 ? "top" : d.X > 0 ? "right" : "left";

        private static ElbowTurn? Turn(string a, string b)
        {
            var key = string.CompareOrdinal(a, b) < 0 ? a + "," + b : b + "," + a;
            return key switch
            {
                "right,top" => ElbowTurn.TopRight,
                "bottom,right" => ElbowTurn.RightBottom,
                "bottom,left" => ElbowTurn.BottomLeft,
                "left,top" => ElbowTurn.LeftTop,
                _ => null,
            };
        }
    }

    // One line of a page: a row of up to three cards, or a chapter bar.
    public readonly struct PageRow
    {
        private PageRow(bool isGate, int row, int era, IReadOnlyList<string> slots)
        {
            IsGate = isGate;
            Row = row;
            Era = era;
            Slots = slots;
        }

        public bool IsGate { get; }

        // The authored row (cards only).
        public int Row { get; }

        public int Era { get; }

        // A technology's id per column, or null.
        public IReadOnlyList<string> Slots { get; }

        public static PageRow Gate(int era) => new(true, -1, era, Array.Empty<string>());

        public static PageRow Cards(int row, int era, IReadOnlyList<string> slots) => new(false, row, era, slots);
    }

    public enum EdgePieceKind
    {
        Vertical,
        Horizontal,
        Elbow,
        Head,
    }

    // Which two sides of its box an elbow joins: the art joins top to right and is turned for the other three.
    public enum ElbowTurn
    {
        TopRight,
        RightBottom,
        BottomLeft,
        LeftTop,
    }

    // A straight run whose centre line starts at (X, Y) and runs Length down or right; an elbow's corner; or
    // the head's tip, pointing down.
    public readonly struct EdgePiece
    {
        public EdgePiece(EdgePieceKind kind, float x, float y, float length, ElbowTurn turn = ElbowTurn.TopRight)
        {
            Kind = kind;
            X = x;
            Y = y;
            Length = length;
            Turn = turn;
        }

        public EdgePieceKind Kind { get; }
        public float X { get; }
        public float Y { get; }
        public float Length { get; }
        public ElbowTurn Turn { get; }
    }
}
