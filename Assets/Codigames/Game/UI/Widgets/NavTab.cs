using System;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Widgets
{
    // One tab of the nav bar: a wooden plate with its mark and its name, or a brass padlock while its door is
    // shut. View only.
    public class NavTab : MonoBehaviour
    {
        [SerializeField] private string _id;
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _face;
        [SerializeField] private GameObject _padlock;
        [SerializeField, Tooltip("The orb with a count: presses worth making behind this door.")] private GameObject _badge;
        [SerializeField] private TMPro.TMP_Text _badgeCount;

        public event Action Tapped;

        public string Id => _id;

        public void SetLocked(bool locked)
        {
            _face.SetActive(!locked);
            _padlock.SetActive(locked);
        }

        public void SetBadge(int count)
        {
            if (_badge == null) return;

            _badge.SetActive(count > 0);
            if (_badgeCount != null) _badgeCount.text = count > 9 ? "9+" : count.ToString();
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);

        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);

        private void OnTapped() => Tapped?.Invoke();
    }
}
