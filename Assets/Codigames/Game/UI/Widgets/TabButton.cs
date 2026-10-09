using System;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Widgets
{
    // A tab inside a window: a wooden plate that stays pressed into the wood while its page is open. View only.
    public class TabButton : MonoBehaviour
    {
        [SerializeField] private string _id;
        [SerializeField] private Button _button;
        [SerializeField] private Image _plate;
        [SerializeField] private Sprite _up;
        [SerializeField] private Sprite _down;
        [SerializeField] private RectTransform _content;
        [SerializeField, Tooltip("How far the face sinks while open, in reference pixels.")] private float _press = 8;
        [SerializeField, Tooltip("How many of the tab's buildings can be built now.")] private Kit.CtaBadge _badge;
        [SerializeField, Tooltip("Optional: a tab whose words are set from code.")] private TMPro.TMP_Text _label;
        [SerializeField, Tooltip("An empty tab, darkened (the web's is-empty brightness).")] private Color _dimmed = new Color32(0xd9, 0xd9, 0xd9, 0xff);
        [SerializeField, Tooltip("Optional: an empty tab's dulled wood (the web's is-empty saturate).")] private Material _dull;

        public event Action Tapped;

        public string Id => _id;

        public void SetBadge(int count)
        {
            if (_badge != null) _badge.Show(count);
        }

        public void SetLabel(string label)
        {
            if (_label != null) _label.text = label;
        }

        public void SetDim(bool dim)
        {
            _plate.color = dim ? _dimmed : Color.white;
            _plate.material = dim ? _dull : null;
        }

        public void SetOpen(bool open)
        {
            _plate.sprite = open ? _down : _up;
            _content.anchoredPosition = new Vector2(0, open ? -_press : 0);
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);

        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);

        private void OnTapped() => Tapped?.Invoke();
    }
}
