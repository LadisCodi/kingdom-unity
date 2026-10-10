using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Quests;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.City
{
    // What buildings standing side by side do to each other (Docs/features/03-economy.md §3.1). Neighbours share an
    // edge of their footprints — a corner is not enough — and one neighbour counts once however long the edge. Only
    // standing neighbours count. A rate (Gold a minute) is read as it is; a time is held within ±25%.
    public class Adjacency
    {
        public const double CLAMP = 0.25;

        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IAdjacencyRules _rules;
        private readonly IBuildingGroups _groups;

        public Adjacency(CityState city, ICatalog<IBuildingDefinition> buildings, IAdjacencyRules rules, IBuildingGroups groups)
        {
            _city = city;
            _buildings = buildings;
            _rules = rules;
            _groups = groups;
        }

        // What a building of a kind would receive at an anchor: summed over its standing neighbours and every rule
        // that matches each.
        public double Effect(string definitionId, Vector2Int anchor, AdjacencyStat stat, string exclude = null)
        {
            // Walked by hand, in the same order as Neighbours and RulesFor: every store's rent asks it, every frame.
            var building = _buildings.Get(definitionId);
            var total = 0.0;
            foreach (var neighbour in _city.Districts)
            {
                if (!neighbour.Built || neighbour.Id == exclude) continue;
                var other = _buildings.Get(neighbour.DefinitionId);
                if (!ShareEdge(anchor, building.Width, building.Height, neighbour.Anchor, other.Width, other.Height)) continue;
                var sum = 0.0;
                var rules = _rules.Rules;
                for (var i = 0; i < rules.Count; i++)
                    if (rules[i] is var rule && rule.Stat == stat && Matches(rule.District, definitionId) && Matches(rule.Neighbor, neighbour.DefinitionId)) sum += rule.Magnitude;
                total += sum;
            }

            return stat == AdjacencyStat.GoldPerMinute ? total : Math.Max(-CLAMP, Math.Min(CLAMP, total));
        }

        // A standing district's Gold a minute from its neighbours.
        public double GoldOf(DistrictState district)
            => Effect(district.DefinitionId, district.Anchor, AdjacencyStat.GoldPerMinute, district.Id);

        // A time's multiplier for a district: 0.9 for 10% faster.
        public double Multiplier(DistrictState district, AdjacencyStat stat)
            => 1 + Effect(district.DefinitionId, district.Anchor, stat, district.Id);

        // Every stat a district's neighbours move, with its total.
        public IReadOnlyList<(AdjacencyStat Stat, double Total)> InEffect(DistrictState district)
            => _rules.Rules.Select(r => r.Stat).Distinct()
                .Select(s => (s, Effect(district.DefinitionId, district.Anchor, s, district.Id)))
                .Where(e => e.Item2 != 0).ToList();

        // A building held over a plot: what each standing neighbour would gain from it, rule by rule, and what it
        // would receive itself, stat by stat. Moving, `exclude` is the mover, which is not its own neighbour.
        public AdjacencyPreview Preview(string definitionId, Vector2Int anchor, string exclude = null)
        {
            var building = _buildings.Get(definitionId);
            var given = new List<(DistrictState District, AdjacencyStat Stat, double Magnitude)>();
            foreach (var neighbour in Neighbours(anchor, building.Width, building.Height, exclude))
            {
                foreach (var rule in _rules.Rules.Where(r => Matches(r.District, neighbour.DefinitionId) && Matches(r.Neighbor, definitionId)))
                    given.Add((neighbour, rule.Stat, rule.Magnitude));
            }

            var received = _rules.Rules.Select(r => r.Stat).Distinct()
                .Select(s => (s, Effect(definitionId, anchor, s, exclude))).Where(e => e.Item2 != 0).ToList();
            return new AdjacencyPreview(given, received);
        }

        // Footprints sharing an edge: touching along one axis, overlapping along the other.
        public static bool ShareEdge(Vector2Int a, int aw, int ah, Vector2Int b, int bw, int bh)
        {
            var dx = Math.Max(Math.Max(b.X - (a.X + aw - 1), a.X - (b.X + bw - 1)), 0);
            var dy = Math.Max(Math.Max(b.Y - (a.Y + ah - 1), a.Y - (b.Y + bh - 1)), 0);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        private IEnumerable<DistrictState> Neighbours(Vector2Int anchor, int width, int height, string exclude)
        {
            foreach (var district in _city.Districts)
            {
                if (!district.Built || district.Id == exclude) continue;
                var other = _buildings.Get(district.DefinitionId);
                if (ShareEdge(anchor, width, height, district.Anchor, other.Width, other.Height)) yield return district;
            }
        }

        private bool Matches(string side, string definitionId) => side == definitionId || _groups.Names(side, definitionId);
    }

    public sealed class AdjacencyPreview
    {
        public AdjacencyPreview(IReadOnlyList<(DistrictState District, AdjacencyStat Stat, double Magnitude)> given,
            IReadOnlyList<(AdjacencyStat Stat, double Total)> received)
        {
            Given = given;
            Received = received;
        }

        public IReadOnlyList<(DistrictState District, AdjacencyStat Stat, double Magnitude)> Given { get; }
        public IReadOnlyList<(AdjacencyStat Stat, double Total)> Received { get; }
    }
}
