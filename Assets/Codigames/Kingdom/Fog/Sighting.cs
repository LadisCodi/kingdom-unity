using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Fog.State;
using Codigames.Kingdom.Map;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Sites.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Fog
{
    // A tall thing past the fog — a big mountain, a landmark, a lair, a ruin — shows as a silhouette while revealed ground
    // lies close enough to it. A fact read off the revealed cells, which only grow, so nothing is stored and nothing
    // is scheduled. A sighting is not a discovery: it announces nothing and finds nothing.
    public class Sighting
    {
        private static readonly HashSet<string> MOUNTAINS = new() { "Mountain", "MountainIron", "MountainGold" };

        private readonly FogState _state;
        private readonly FogOfWar _fog;
        private readonly SitesState _sites;
        private readonly Lairs.State.LairsState _lairs;
        private readonly List<(Sighted Thing, int Range)> _candidates = new();

        private (int Revealed, int Discovered, int Repaired, int Lairs) _stamp = (-1, -1, -1, -1);
        private List<Sighted> _sighted = new();

        public Sighting(FogState state, FogOfWar fog, IProvinceMap map, Footprints footprints, IProvinceSites sites, SitesState sitesState,
            ICatalog<IBuildingDefinition> buildings, ISightSettings settings, Lairs.State.LairsState lairs = null)
        {
            _lairs = lairs;
            _state = state;
            _fog = fog;
            _sites = sitesState;

            foreach (var (anchor, size) in footprints.Blocks)
            {
                var feature = map.FeatureAt(anchor);
                var range = size - 1 < settings.MountainBySize.Count ? settings.MountainBySize[size - 1] : 0;
                if (feature != null && MOUNTAINS.Contains(feature) && range > 0)
                    _candidates.Add((new Sighted(SightedKind.Mountain, feature, anchor, size), range));
            }

            foreach (var landmark in sites.Landmarks.Where(_ => settings.Landmark > 0))
                _candidates.Add((new Sighted(SightedKind.Landmark, landmark.Id, landmark.Anchor, landmark.Size), settings.Landmark));

            foreach (var lair in sites.Lairs.Where(l => l.Sight > 0))
                _candidates.Add((new Sighted(SightedKind.Lair, lair.Id, lair.Anchor, lair.Size), lair.Sight));

            // An abandoned building shows as the silhouette of its ruin: something stands there, not what.
            foreach (var ruin in sites.Abandoned.Where(r => r.Sight > 0))
            {
                var building = buildings.Get(ruin.District);
                _candidates.Add((new Sighted(SightedKind.Abandoned, ruin.Id, ruin.Anchor, System.Math.Max(building.Width, building.Height)), ruin.Sight));
            }
        }

        // Everything sighted now and not yet in plain view.
        public IReadOnlyList<Sighted> Things
        {
            get
            {
                var stamp = (_state.Revealed.Count, _state.Discovered.Count, _sites.Repaired.Count, _lairs?.Lairs.Count ?? 0);
                if (stamp == _stamp) return _sighted;

                _stamp = stamp;
                _sighted = _candidates.Where(c => !InView(c.Thing) && SeenFrom(c.Thing, c.Range)).Select(c => c.Thing).ToList();
                return _sighted;
            }
        }

        // The sighted thing standing on a cell; null when none is.
        public Sighted? At(Vector2Int cell)
        {
            foreach (var thing in Things)
            {
                if (thing.Covers(cell)) return thing;
            }

            return null;
        }

        // Measured from revealed cells only (Chebyshev), to the nearest cell of its footprint.
        private bool SeenFrom(Sighted thing, int range)
        {
            for (var y = thing.Anchor.Y - range; y < thing.Anchor.Y + thing.Size + range; y++)
            {
                for (var x = thing.Anchor.X - range; x < thing.Anchor.X + thing.Size + range; x++)
                {
                    if (_state.Revealed.Contains(new Vector2Int(x, y))) return true;
                }
            }

            return false;
        }

        // Drawn as itself once any of it is seen; a repaired ruin is a building; a lair once found, whatever the fog on it.
        private bool InView(Sighted thing)
        {
            if (thing.Kind == SightedKind.Lair) return _lairs?.Lairs.ContainsKey(thing.Id) ?? false;
            if (thing.Kind == SightedKind.Abandoned && _sites.Repaired.Contains(thing.Id)) return true;

            for (var y = thing.Anchor.Y; y < thing.Anchor.Y + thing.Size; y++)
            {
                for (var x = thing.Anchor.X; x < thing.Anchor.X + thing.Size; x++)
                {
                    if (_fog.VisibilityAt(new Vector2Int(x, y)) != Visibility.Undiscovered) return true;
                }
            }

            return false;
        }
    }
}
