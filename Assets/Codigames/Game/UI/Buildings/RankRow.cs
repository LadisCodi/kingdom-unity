using System;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Buildings
{
    // A rank in a hall's menu (the web's rankMenu row): its portrait and coin, its name, why it is shut, its numbers.
    public class RankRow : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private UnitPortrait _portrait;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _locked;
        [SerializeField] private TMP_Text _numbers;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private GameObject _picked;

        private string _troop;

        public event Action<string> Tapped;

        public void Show(RankRowData row)
        {
            _troop = row.Troop;
            _portrait.Show(row.Bust, row.BustShift, row.BustScale, string.Empty);
            _portrait.ShowRank(row.Rank);
            _name.text = row.Name;
            _locked.gameObject.SetActive(!string.IsNullOrEmpty(row.Locked));
            _locked.text = row.Locked;
            _numbers.text = row.Numbers;
            _group.alpha = string.IsNullOrEmpty(row.Locked) ? 1 : 0.55f;
            _picked.SetActive(row.Picked);
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);
        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);
        private void OnTapped() => Tapped?.Invoke(_troop);
    }
}
