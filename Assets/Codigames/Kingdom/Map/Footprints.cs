using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Harvest;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Map
{
    // Things on the province that are one object over a square of cells: a mountain is grouped from the cells
    // painted with it (greedy 3 × 3, then 2 × 2, row by row — the same cells give the same blocks every time,
    // so a save never meets a map that moved under it); a site declares its own size. A block is discovered
    // when any of its cells is, cleared all at once for the sum of its cells, and drawn once.
    public class Footprints
    {
        private readonly Dictionary<Vector2Int, Vector2Int> _anchorOf = new();
        private readonly Dictionary<Vector2Int, int> _sizeOf = new();

        public Footprints(IProvinceMap map, ICatalog<IFeatureDefinition> features, IEnumerable<(Vector2Int Anchor, int Size)> sites = null)
        {
            // Per feature, so a mountain never joins an iron one into a block that is neither.
            foreach (var feature in features.Items.Where(f => f.MaxFootprint > 1))
            {
                var painted = map.Cells.Where(c => map.FeatureAt(c) == feature.Id);
                foreach (var (anchor, size) in Group(painted, feature.MaxFootprint)) Add(anchor, size);
            }

            foreach (var (anchor, size) in sites ?? Enumerable.Empty<(Vector2Int, int)>()) Add(anchor, size);
        }

        // Every block of more than one cell: its top-left cell and its side.
        public IEnumerable<(Vector2Int Anchor, int Size)> Blocks => _sizeOf.Select(b => (b.Key, b.Value));

        // The block a cell belongs to: its own cell, a side of one, when it is in none.
        public Vector2Int AnchorOf(Vector2Int cell) => _anchorOf.TryGetValue(cell, out var anchor) ? anchor : cell;

        public int SizeOf(Vector2Int cell) => _sizeOf.TryGetValue(AnchorOf(cell), out var size) ? size : 1;

        public IEnumerable<Vector2Int> CellsOf(Vector2Int cell) => Square(AnchorOf(cell), SizeOf(cell));

        // Square blocks out of the given cells, the largest first, scanning row by row and left to right; whatever
        // no block could take is its own cell.
        public static List<(Vector2Int Anchor, int Size)> Group(IEnumerable<Vector2Int> cells, int maxSize)
        {
            var free = new HashSet<Vector2Int>(cells);
            var order = free.OrderBy(c => c.Y).ThenBy(c => c.X).ToList();
            var blocks = new List<(Vector2Int, int)>();

            for (var size = Math.Max(1, maxSize); size >= 2; size--)
            {
                foreach (var anchor in order)
                {
                    if (!free.Contains(anchor)) continue;

                    var block = Square(anchor, size).ToList();
                    if (!block.All(free.Contains)) continue;

                    foreach (var cell in block) free.Remove(cell);
                    blocks.Add((anchor, size));
                }
            }

            foreach (var anchor in order)
            {
                if (free.Remove(anchor)) blocks.Add((anchor, 1));
            }

            return blocks;
        }

        private void Add(Vector2Int anchor, int size)
        {
            if (size <= 1) return;

            _sizeOf[anchor] = size;
            foreach (var cell in Square(anchor, size)) _anchorOf[cell] = anchor;
        }

        private static IEnumerable<Vector2Int> Square(Vector2Int anchor, int size)
        {
            for (var dy = 0; dy < size; dy++)
            {
                for (var dx = 0; dx < size; dx++) yield return new Vector2Int(anchor.X + dx, anchor.Y + dy);
            }
        }
    }
}
