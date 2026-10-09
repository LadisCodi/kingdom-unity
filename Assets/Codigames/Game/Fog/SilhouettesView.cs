using System;
using System.Collections.Generic;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Harvest;
using Codigames.Game.Data.Sites;
using Codigames.Game.Map;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Sites;
using UnityEngine;
using VContainer.Unity;
using Vector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Fog
{
    // What is sighted past the fog, drawn over the cloud tops as its own drawing in one flat, cold, faint colour —
    // no name, no badge: something stands there. Gone once it is seen and drawn as itself.
    public class SilhouettesView : IStartable, IDisposable
    {
        // Over the clouds, which are drawn over everything on the ground.
        private const int SORTING_ORDER = 600;

        private readonly Sighting _sighting;
        private readonly FogOfWar _fog;
        private readonly ProvinceMap _map;
        private readonly FeatureCollection _features;
        private readonly ProvinceSitesAsset _sites;
        private readonly BuildingCollection _buildings;
        private readonly Material _material;
        private readonly Dictionary<string, SpriteRenderer> _drawn = new();

        private Transform _parent;

        public SilhouettesView(Sighting sighting, FogOfWar fog, ProvinceMap map, FeatureCollection features, ProvinceSitesAsset sites,
            BuildingCollection buildings, SightArt art)
        {
            _sighting = sighting;
            _fog = fog;
            _map = map;
            _features = features;
            _sites = sites;
            _buildings = buildings;
            _material = art.Silhouette;
        }

        public void Start()
        {
            _parent = new GameObject("Silhouettes").transform;
            Refresh();
            _fog.Changed += OnFogChanged;
        }

        public void Dispose() => _fog.Changed -= OnFogChanged;

        private void OnFogChanged(IReadOnlyCollection<Vector2Int> cells) => Refresh();

        private void Refresh()
        {
            var shown = new HashSet<string>();
            foreach (var thing in _sighting.Things)
            {
                var key = $"{thing.Kind}:{thing.Id}:{thing.Anchor.X},{thing.Anchor.Y}";
                shown.Add(key);
                if (!_drawn.ContainsKey(key)) _drawn[key] = Draw(thing, key);
            }

            foreach (var key in new List<string>(_drawn.Keys))
            {
                if (shown.Contains(key)) continue;

                if (_drawn[key] != null) UnityEngine.Object.Destroy(_drawn[key].gameObject);
                _drawn.Remove(key);
            }
        }

        private SpriteRenderer Draw(Sighted thing, string key)
        {
            var art = new GameObject(key).AddComponent<SpriteRenderer>();
            art.transform.SetParent(_parent, false);
            art.sharedMaterial = _material;
            art.sortingOrder = SORTING_ORDER;

            var bottom = new Vector2Int(thing.Anchor.X + thing.Size - 1, thing.Anchor.Y + thing.Size - 1);
            var corner = _map.CellCentre(bottom) - new Vector3(0f, _map.Grid.cellSize.y / 2f, 0f);
            switch (thing.Kind)
            {
                case SightedKind.Mountain:
                    // Drawn as the feature's tile draws it: on its 2-plot canvas, feet on the footprint's bottom corner.
                    art.sprite = _features.TryGet(thing.Id, out var definition) && definition is FeatureAsset feature
                        ? (feature.TileFor(thing.Size) as VariantTile)?.SpriteAt(thing.Anchor)
                        : null;
                    art.transform.position = corner;
                    break;
                case SightedKind.Landmark:
                    var site = _sites.Landmarks.Find(thing.Id);
                    art.sprite = site == null ? null : _sites.KindOf(site.Kind)?.Art;
                    art.transform.position = corner;
                    break;
                case SightedKind.Lair:
                    // A feature's two plots across its footprint, feet on its bottom corner.
                    var lair = _sites.LairOf(thing.Id);
                    art.sprite = lair?.Model;
                    art.transform.position = corner;
                    art.transform.localScale = Vector3.one * thing.Size;
                    break;
                default:
                    // A ruin is building art: one plot across, sized to its footprint.
                    var ruin = _sites.Abandoned.Find(thing.Id);
                    var building = ruin == null ? null : _buildings.Get<BuildingAsset>(ruin.District);
                    art.sprite = building?.RuinArt;
                    if (building != null)
                    {
                        var (basePosition, width) = ProvinceGeometry.Footprint(_map, ruin.Anchor, building.Width, building.Height);
                        art.transform.position = basePosition;
                        if (art.sprite != null) art.transform.localScale = Vector3.one * (width / (art.sprite.rect.width / art.sprite.pixelsPerUnit));
                    }

                    break;
            }

            return art;
        }
    }
}
