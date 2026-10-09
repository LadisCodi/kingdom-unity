using System;
using System.Collections.Generic;
using Codigames.Game.Data.City;
using Codigames.Game.Map;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Sites;
using Vector2Int = Codigames.Modules.Core.Vector2Int;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.Sites
{
    // The abandoned buildings on the map: each its building's level 1 in ruin, standing on its footprint like a
    // building; drained under the veil while only discovered, not drawn under the cloud bank, gone once its
    // repair starts (the building it was takes its place).
    public class RuinsView : IStartable, IDisposable
    {
        private const int SORTING_ORDER = 100;
        private static readonly Color DISCOVERED = new(0.8f, 0.8f, 0.88f, 1f);

        private readonly Ruins _ruins;
        private readonly SiteGround _ground;
        private readonly FogOfWar _fog;
        private readonly ProvinceMap _map;
        private readonly BuildingCollection _buildings;
        private readonly Dictionary<string, SpriteRenderer> _drawn = new();

        private Transform _parent;

        public RuinsView(Ruins ruins, SiteGround ground, FogOfWar fog, ProvinceMap map, BuildingCollection buildings)
        {
            _ruins = ruins;
            _ground = ground;
            _fog = fog;
            _map = map;
            _buildings = buildings;
        }

        public void Start()
        {
            _parent = new GameObject("Ruins").transform;
            Refresh();
            _fog.Changed += OnFogChanged;
            _ruins.Repaired += OnRepaired;
        }

        public void Dispose()
        {
            _fog.Changed -= OnFogChanged;
            _ruins.Repaired -= OnRepaired;
        }

        private void OnFogChanged(IReadOnlyCollection<Vector2Int> cells) => Refresh();

        private void OnRepaired(IAbandonedSite site)
        {
            if (!_drawn.TryGetValue(site.Id, out var art)) return;

            UnityEngine.Object.Destroy(art.gameObject);
            _drawn.Remove(site.Id);
        }

        private void Refresh()
        {
            foreach (var site in _ruins.Standing)
            {
                var visibility = _fog.VisibilityAt(site.Anchor);
                var art = Art(site);
                art.gameObject.SetActive(visibility != Visibility.Undiscovered);
                art.color = visibility == Visibility.Discovered ? DISCOVERED : Color.white;
            }
        }

        private SpriteRenderer Art(IAbandonedSite site)
        {
            if (_drawn.TryGetValue(site.Id, out var art)) return art;

            var building = _buildings.Get<BuildingAsset>(site.District);
            var (basePosition, width) = ProvinceGeometry.Footprint(_map, site.Anchor, building.Width, building.Height);
            art = new GameObject(site.Id).AddComponent<SpriteRenderer>();
            art.transform.SetParent(_parent, false);
            art.transform.position = basePosition;
            art.sprite = building.RuinArt != null ? building.RuinArt : building.ArtFor(1);
            art.sortingOrder = SORTING_ORDER;
            if (art.sprite != null) art.transform.localScale = Vector3.one * (width / (art.sprite.rect.width / art.sprite.pixelsPerUnit));

            _drawn[site.Id] = art;
            return art;
        }
    }
}
