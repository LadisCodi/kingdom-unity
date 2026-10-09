using System.Collections.Generic;
using Codigames.Game.UI.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Kit
{
    // A band of stat tiles (the web's dc-stats): fixed-size tiles three to a row, each row centred — a fourth wraps to
    // a row of its own, still centred. A relic's band is two wider tiles a row. Rows and tiles are kept and reused.
    public class StatBand : MonoBehaviour
    {
        [SerializeField] private StatTile _tilePrefab;
        [SerializeField] private float _gap = 28;
        [SerializeField, Min(1)] private int _perRow = 3;
        [SerializeField, Tooltip("0: the tile prefab's own width.")] private float _tileWidth;

        private readonly List<RectTransform> _rows = new();
        private readonly List<StatTile> _tiles = new();

        public void Show(IReadOnlyList<StatTileData> stats)
        {
            gameObject.SetActive(stats.Count > 0);
            var rows = (stats.Count + _perRow - 1) / _perRow;
            while (_rows.Count < rows) _rows.Add(NewRow());

            for (var i = 0; i < stats.Count; i++)
            {
                if (i == _tiles.Count) _tiles.Add(NewTile());
                var tile = _tiles[i];
                tile.transform.SetParent(_rows[i / _perRow], false);
                tile.gameObject.SetActive(true);
                tile.Show(stats[i].Icon, stats[i].Label, stats[i].Value, stats[i].Bad);
            }

            for (var i = stats.Count; i < _tiles.Count; i++) _tiles[i].gameObject.SetActive(false);
            for (var i = 0; i < _rows.Count; i++) _rows[i].gameObject.SetActive(i < rows);
        }

        private StatTile NewTile()
        {
            var tile = Instantiate(_tilePrefab);
            if (_tileWidth > 0 && tile.TryGetComponent<LayoutElement>(out var element)) element.minWidth = element.preferredWidth = _tileWidth;
            return tile;
        }

        private RectTransform NewRow()
        {
            var row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(transform, false);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = _gap;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            return row;
        }
    }
}
