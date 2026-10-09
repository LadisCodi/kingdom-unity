using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Map;
using Codigames.Kingdom.Sites;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Tutorial
{
    // WHAT A LINE POINTS AT ON THE MAP (Docs/features/24-dialogue.md §4): a line's `point` resolved to a plot. A
    // target is resolved once and kept while it still answers the point: a pointer that hopped to the next tree
    // every frame would chase the player's thumb.
    //   feature:<id>          the nearest cell carrying it out of the dark
    //   feature:<id>Fog       the nearest fogged one the player can pay for — or the step towards the nearest
    //   feature:<id>Revealed  the nearest revealed one with something left to take
    //   district:<id>         one of the kind, a built one first
    //   crew:<id>             the one of its kind with the most room for hands
    //   lowest:<id>           the one of its kind furthest behind
    //   idle: / full:         a crew with hands to spare; a store that has stopped
    //   abandoned:<id>[Fog]   a ruin; with Fog, the way to it through the fog
    //   landmark:[<id>]       a landmark; alone, the nearest unclaimed one out of the dark
    //   reach:<id>            where one more of the building would stand
    //   treasure              the nearest treasure on the ground
    //   cell:<x>,<y>          that cell
    public class MapTargets
    {
        private const string FOG = "Fog";
        private const string REVEALED = "Revealed";

        private readonly KingdomState _state;
        private readonly IProvinceMap _map;
        private readonly FogOfWar _fog;
        private readonly Harvesting _harvesting;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IProvinceSites _sites;
        private readonly Workforce _crews;
        private readonly Stores _stores;
        private readonly Placement _placement;

        public MapTargets(KingdomState state, IProvinceMap map, FogOfWar fog, Harvesting harvesting, ICatalog<IBuildingDefinition> buildings,
            IProvinceSites sites, Workforce crews, Stores stores, Placement placement)
        {
            _state = state;
            _map = map;
            _fog = fog;
            _harvesting = harvesting;
            _buildings = buildings;
            _sites = sites;
            _crews = crews;
            _stores = stores;
            _placement = placement;
        }

        private CityState City => _state.City;

        // Is this a point on the map (rather than a control, or nothing)?
        public static bool IsMapPoint(string point)
            => !string.IsNullOrEmpty(point) && point != "quest" && point != "back" && !point.StartsWith("ui:") && !point.StartsWith("hex:");

        // The plot `point` names now; `previous` is kept while it still answers. Null when nothing fits yet.
        public MapTarget? Resolve(string point, MapTarget? previous, double now)
        {
            if (!IsMapPoint(point)) return null;
            if (previous.HasValue && StillGood(point, previous.Value.Anchor, now)) return previous;

            var colon = point.IndexOf(':');
            var kind = colon < 0 ? point : point.Substring(0, colon);
            var id = colon < 0 ? "" : point.Substring(colon + 1);
            switch (kind)
            {
                case "cell":
                {
                    var xy = id.Split(',');
                    return xy.Length == 2 && int.TryParse(xy[0], out var x) && int.TryParse(xy[1], out var y)
                        ? new MapTarget(new Vector2Int(x, y))
                        : null;
                }
                case "feature":
                {
                    var cell = Nearest(c => StillGood(point, c, now)) ?? Towards(id);
                    return cell.HasValue ? new MapTarget(cell.Value) : null;
                }
                case "district":
                {
                    var all = City.Districts.Where(d => d.DefinitionId == id).ToList();
                    return Plot(all.FirstOrDefault(d => d.Built) ?? all.FirstOrDefault());
                }
                case "crew":
                    return Plot(City.Districts.Where(d => d.DefinitionId == id && d.Built)
                        .OrderByDescending(d => _crews.Limit(d) - _crews.Assigned(d.Id)).FirstOrDefault());
                case "lowest":
                    return Plot(City.Districts.Where(d => d.DefinitionId == id && d.Built).OrderBy(d => d.Level).FirstOrDefault());
                case "idle":
                    return Plot(City.Districts.FirstOrDefault(d => d.Built && _crews.Assigned(d.Id) > 0
                        && _crews.Assigned(d.Id) > _crews.Workable(d).Count));
                case "full":
                    return Plot(City.Districts.FirstOrDefault(d => d.Built && _stores.IsFull(d, now)));
                case "landmark":
                {
                    var landmark = id != ""
                        ? _sites.Landmarks.FirstOrDefault(l => l.Id == id)
                        : _sites.Landmarks.Where(l => !_state.Sites.Claimed.Contains(l.Id) && _fog.VisibilityAt(l.Anchor) != Visibility.Undiscovered)
                            .OrderBy(l => _fog.Rings(l.Anchor)).FirstOrDefault();
                    return landmark == null ? null : new MapTarget(landmark.Anchor, landmark.Size, landmark.Size);
                }
                case "abandoned":
                    return Abandoned(id);
                case "reach":
                {
                    if (!_buildings.TryGet(id, out var building)) return null;
                    var spot = _placement.Nearest(id);
                    return spot.HasValue ? new MapTarget(spot.Value, building.Width, building.Height) : null;
                }
                case "treasure":
                {
                    var cell = Nearest(c => _state.Fog.Treasures.ContainsKey(c));
                    return cell.HasValue ? new MapTarget(cell.Value) : null;
                }
                default:
                    return null;
            }
        }

        // Does `cell` still answer the point it was resolved for?
        private bool StillGood(string point, Vector2Int cell, double now)
        {
            // The way to a ruin is a step of the way: good until it is cleared.
            if (point.StartsWith("abandoned:") && point.EndsWith(FOG)) return Buyable(cell);
            if (!point.StartsWith("feature:")) return true;

            var want = point.Substring("feature:".Length);
            var revealed = want.EndsWith(REVEALED);
            var fogged = !revealed && want.EndsWith(FOG);
            var id = revealed ? want.Substring(0, want.Length - REVEALED.Length) : fogged ? want.Substring(0, want.Length - FOG.Length) : want;
            if (!_state.Ground.Features.TryGetValue(cell, out var feature) || feature != id) return false;
            if (revealed) return _fog.IsRevealed(cell) && _harvesting.SourceAt(cell) != null && !_harvesting.IsExhausted(cell, now);
            if (fogged) return Buyable(cell);
            return _fog.VisibilityAt(cell) != Visibility.Undiscovered;
        }

        // Can the player pay the fog off the cell now?
        private bool Buyable(Vector2Int cell)
            => _fog.VisibilityAt(cell) == Visibility.Discovered && _fog.IsPayable(cell) && _fog.TerrainTech(cell) == null;

        // `feature:<id>Fog` with nothing of the kind payable: the payable cell that leads towards the nearest one.
        private Vector2Int? Towards(string want)
        {
            if (!want.EndsWith(FOG)) return null;
            var id = want.Substring(0, want.Length - FOG.Length);
            var goal = Nearest(c => _state.Ground.Features.TryGetValue(c, out var f) && f == id && !_fog.IsRevealed(c));
            return goal.HasValue ? StepTowards(goal.Value) : null;
        }

        // The payable cell nearest `goal`: straight lines before diagonals.
        private Vector2Int? StepTowards(Vector2Int goal)
        {
            Vector2Int? best = null;
            var bestDistance = int.MaxValue;
            foreach (var c in _map.Cells)
            {
                if (!Buyable(c)) continue;
                var dx = Math.Abs(c.X - goal.X);
                var dy = Math.Abs(c.Y - goal.Y);
                var distance = Math.Max(dx, dy) * 100 + dx + dy;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = c;
            }

            return best;
        }

        // A ruin wherever the fog has it; with Fog, the ruin once its fog can be paid, until then the step towards it.
        private MapTarget? Abandoned(string id)
        {
            var way = id.EndsWith(FOG);
            var site = _sites.Abandoned.FirstOrDefault(a => a.Id == (way ? id.Substring(0, id.Length - FOG.Length) : id));
            if (site == null || !_buildings.TryGet(site.District, out var building)) return null;

            var plot = new MapTarget(site.Anchor, building.Width, building.Height);
            if (!way || _fog.IsRevealed(site.Anchor) || Buyable(site.Anchor)) return plot;
            var step = StepTowards(site.Anchor);
            return step.HasValue ? new MapTarget(step.Value) : plot;
        }

        private MapTarget? Plot(DistrictState district)
        {
            if (district == null) return null;
            var building = _buildings.Get(district.DefinitionId);
            return new MapTarget(district.Anchor, building.Width, building.Height);
        }

        // The cell nearest the Townhall that matches; ties to the lowest row, then column.
        private Vector2Int? Nearest(Func<Vector2Int, bool> match)
        {
            Vector2Int? best = null;
            var bestRings = int.MaxValue;
            foreach (var c in _map.Cells)
            {
                if (!match(c)) continue;
                var rings = _fog.Rings(c);
                if (rings > bestRings || (rings == bestRings && best.HasValue && Before(best.Value, c))) continue;
                bestRings = rings;
                best = c;
            }

            return best;
        }

        private static bool Before(Vector2Int a, Vector2Int b) => a.Y < b.Y || (a.Y == b.Y && a.X <= b.X);
    }
}
