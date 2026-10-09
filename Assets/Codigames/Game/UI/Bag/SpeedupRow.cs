using System;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Bag
{
    // One speed-up that fits the timer (the web's spd-row): its tile, its name, and Use.
    public class SpeedupRow : MonoBehaviour
    {
        [SerializeField] private BagTile _tile;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private KitButton _use;

        public event Action<string> UseTapped;

        public string Id { get; private set; }

        public void Show(SpeedupRowData row, string use)
        {
            _use.Label = use;
            Id = row.Tile.Id;
            _tile.Show(row.Tile);
            _name.text = row.Name;
        }

        private void OnEnable() => _use.onClick.AddListener(OnUse);
        private void OnDisable() => _use.onClick.RemoveListener(OnUse);
        private void OnUse() => UseTapped?.Invoke(Id);
    }
}
