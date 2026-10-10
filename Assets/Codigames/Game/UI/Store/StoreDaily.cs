using System;
using System.Collections.Generic;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Store
{
    // One of today's offers: its name, two of its rewards, its price — or sold out until tomorrow.
    public sealed class StoreDailyData
    {
        public string Name;
        public IReadOnlyList<OfferTileData> Tiles = Array.Empty<OfferTileData>();
        public string Price;
        public bool SoldOut;
    }

    public class StoreDaily : MonoBehaviour
    {
        [SerializeField] private TMP_Text _name;
        [SerializeField] private RectTransform _tiles;
        [SerializeField] private OfferTileView _tilePrefab;
        [SerializeField] private KitButton _buy;

        public event Action Tapped;

        private void Awake() => _buy.onClick.AddListener(() => Tapped?.Invoke());

        public void Show(StoreDailyData data)
        {
            _name.text = data.Name;
            for (var i = _tiles.childCount - 1; i >= 0; i--) Destroy(_tiles.GetChild(i).gameObject);
            foreach (var tile in data.Tiles) Instantiate(_tilePrefab, _tiles).Show(tile);
            _buy.Label = data.Price;
            _buy.interactable = !data.SoldOut;
        }
    }
}
