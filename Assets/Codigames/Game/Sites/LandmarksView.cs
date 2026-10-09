using System;
using System.Collections.Generic;
using Codigames.Game.Data.Sites;
using Codigames.Game.Map;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Sites;
using UnityEngine;
using VContainer.Unity;
using Vector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Sites
{
    // The landmarks on the map, standing on their ground as a feature does: drained under the veil while only
    // discovered, not drawn under the cloud bank.
    public class LandmarksView : IStartable, IDisposable
    {
        private const int SORTING_ORDER = 40;
        private static readonly Color DISCOVERED = new(0.8f, 0.8f, 0.88f, 1f);

        private readonly Landmarks _landmarks;
        private readonly ProvinceSitesAsset _sites;
        private readonly FogOfWar _fog;
        private readonly ProvinceMap _map;
        private readonly Dictionary<string, SpriteRenderer> _drawn = new();

        private Transform _parent;

        public LandmarksView(Landmarks landmarks, ProvinceSitesAsset sites, FogOfWar fog, ProvinceMap map)
        {
            _landmarks = landmarks;
            _sites = sites;
            _fog = fog;
            _map = map;
        }

        public void Start()
        {
            _parent = new GameObject("Landmarks").transform;
            Refresh();
            _fog.Changed += OnFogChanged;
        }

        public void Dispose() => _fog.Changed -= OnFogChanged;

        private void OnFogChanged(IReadOnlyCollection<Vector2Int> cells) => Refresh();

        private void Refresh()
        {
            foreach (var site in _landmarks.All)
            {
                var visibility = _fog.VisibilityAt(site.Anchor);
                var art = Art(site);
                art.gameObject.SetActive(visibility != Visibility.Undiscovered);
                art.color = visibility == Visibility.Discovered ? DISCOVERED : Color.white;
            }
        }

        // Its feet on its footprint's bottom corner, as a feature's are.
        private SpriteRenderer Art(ILandmarkSite site)
        {
            if (_drawn.TryGetValue(site.Id, out var art)) return art;

            var bottom = new Vector2Int(site.Anchor.X + site.Size - 1, site.Anchor.Y + site.Size - 1);
            art = new GameObject(site.Id).AddComponent<SpriteRenderer>();
            art.transform.SetParent(_parent, false);
            art.transform.position = _map.CellCentre(bottom) - new Vector3(0f, _map.Grid.cellSize.y / 2f, 0f);
            art.sprite = _sites.KindOf(site.Kind)?.Art;
            art.sortingOrder = SORTING_ORDER;
            _drawn[site.Id] = art;
            return art;
        }
    }
}
