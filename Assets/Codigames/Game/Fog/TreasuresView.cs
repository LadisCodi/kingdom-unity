using System;
using System.Collections.Generic;
using Codigames.Game.Map;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Fog.State;
using UnityEngine;
using VContainer.Unity;
using Vector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Fog
{
    // The treasures on the map: a closed chest under the mist, standing out of it more than the ground under it; once
    // its cell is revealed, the coin it holds risen out of it, waiting for the tap that picks it up.
    public class TreasuresView : IStartable, IDisposable
    {
        private const int SORTING_ORDER = 50;
        private static readonly Color MISTED = new(0.86f, 0.86f, 0.92f, 1f);

        private readonly Treasures _treasures;
        private readonly FogOfWar _fog;
        private readonly ProvinceMap _map;
        private readonly TreasureArt _art;
        private readonly Dictionary<Vector2Int, (SpriteRenderer Chest, SpriteRenderer Coin)> _drawn = new();

        private Transform _parent;

        public TreasuresView(Treasures treasures, FogOfWar fog, ProvinceMap map, TreasureArt art)
        {
            _treasures = treasures;
            _fog = fog;
            _map = map;
            _art = art;
        }

        public void Start()
        {
            _parent = new GameObject("Treasures").transform;
            Refresh();
            _fog.Changed += OnFogChanged;
            _treasures.Placed += OnPlaced;
            _treasures.PickedUp += OnPickedUp;
        }

        public void Dispose()
        {
            _fog.Changed -= OnFogChanged;
            _treasures.Placed -= OnPlaced;
            _treasures.PickedUp -= OnPickedUp;
        }

        private void OnFogChanged(IReadOnlyCollection<Vector2Int> cells) => Refresh();

        private void OnPlaced(Vector2Int cell, Treasure treasure) => Refresh();

        private void OnPickedUp(Vector2Int cell, IReadOnlyDictionary<string, double> reward)
        {
            if (!_drawn.TryGetValue(cell, out var drawn)) return;

            UnityEngine.Object.Destroy(drawn.Chest.gameObject);
            _drawn.Remove(cell);
        }

        private void Refresh()
        {
            foreach (var (cell, treasure) in _treasures.All)
            {
                var visibility = _fog.VisibilityAt(cell);
                var drawn = Drawn(cell, treasure);
                drawn.Chest.gameObject.SetActive(visibility != Visibility.Undiscovered);
                drawn.Chest.color = visibility == Visibility.Revealed ? Color.white : MISTED;
                drawn.Coin.gameObject.SetActive(visibility == Visibility.Revealed);
            }
        }

        private (SpriteRenderer Chest, SpriteRenderer Coin) Drawn(Vector2Int cell, Treasure treasure)
        {
            if (_drawn.TryGetValue(cell, out var drawn)) return drawn;

            var (basePosition, width) = ProvinceGeometry.Footprint(_map, cell, 1, 1);
            var chest = Sprite("Treasure", _parent, _art.Closed, SORTING_ORDER);
            chest.transform.position = basePosition;
            chest.transform.localScale = Vector3.one * (width / (_art.Closed.rect.width / _art.Closed.pixelsPerUnit));
            var coin = Sprite("Coin", chest.transform, _art.CoinOf(treasure.N == 0 ? "Gold" : treasure.Coin), SORTING_ORDER + 1);

            drawn = (chest, coin);
            _drawn[cell] = drawn;
            return drawn;
        }

        private static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, int order)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
