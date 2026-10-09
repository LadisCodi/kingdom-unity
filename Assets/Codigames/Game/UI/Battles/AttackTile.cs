using System;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Battles
{
    // A troop on the roster: its medallion, how many are left at home, its name under it. Drained when none are left;
    // softer when every slot is full.
    public class AttackTile : MonoBehaviour
    {
        [SerializeField] private UnitPortrait _portrait;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _bust;
        [SerializeField] private Material _drained;
        [SerializeField] private Button _button;

        public event Action Tapped;

        public void Show(Sprite bust, Vector2 shift, float scale, string left, int rank, string name, bool @out, bool full)
        {
            _portrait.Show(bust, shift, scale, left);
            _portrait.ShowRank(rank);
            _name.text = name;
            _bust.material = @out ? _drained : null;
            _group.alpha = @out ? 0.6f : full ? 0.8f : 1f;
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);

        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);

        private void OnTapped() => Tapped?.Invoke();
    }
}
