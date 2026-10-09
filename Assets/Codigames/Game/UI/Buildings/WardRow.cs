using System;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Buildings
{
    // A wounded troop in the Infirmary's beds: who and how many, what mending them is, and Heal priced.
    public class WardRow : MonoBehaviour
    {
        [SerializeField] private UnitPortrait _portrait;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _line;
        [SerializeField] private CostButton _heal;

        private string _troop;

        public event Action<string> HealTapped;

        public void Show(WardRowData row)
        {
            _troop = row.Troop;
            _portrait.Show(row.Bust, row.BustShift, row.BustScale, string.Empty);
            _portrait.ShowRank(row.Rank);
            _name.text = row.Name;
            _line.text = row.Line;
            _heal.Button.Label = row.HealLabel;
            if (string.IsNullOrEmpty(row.Gate)) _heal.Show(row.Price, row.CanHeal);
            else _heal.ShowGate(row.Gate);
        }

        private void OnEnable() => _heal.Button.onClick.AddListener(OnHeal);
        private void OnDisable() => _heal.Button.onClick.RemoveListener(OnHeal);
        private void OnHeal() => HealTapped?.Invoke(_troop);
    }
}
