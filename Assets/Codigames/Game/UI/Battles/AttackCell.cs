using System;
using Codigames.Game.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Battles
{
    // A troop slot on an army board: a squad's round portrait with its count, or the empty carved round. Ours sends its
    // squad home when tapped.
    public class AttackCell : MonoBehaviour
    {
        [SerializeField] private UnitPortrait _portrait;
        [SerializeField] private GameObject _empty;
        [SerializeField] private Button _button;

        public event Action Tapped;

        public void Show(Sprite bust, Vector2 shift, float scale, string count, int rank)
        {
            _portrait.gameObject.SetActive(true);
            _empty.SetActive(false);
            _portrait.Show(bust, shift, scale, count);
            _portrait.ShowRank(rank);
        }

        public void ShowEmpty()
        {
            _portrait.gameObject.SetActive(false);
            _empty.SetActive(true);
        }

        public bool Tappable
        {
            set => _button.interactable = value;
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);

        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);

        private void OnTapped() => Tapped?.Invoke();
    }
}
