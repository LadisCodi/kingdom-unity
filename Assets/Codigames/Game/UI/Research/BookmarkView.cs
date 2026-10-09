using System;
using Codigames.Game.UI.Data.Research;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Research
{
    // A book's ribbon: one ribbon tinted per book, its emblem on it; the open book's hangs longer. View only.
    public class BookmarkView : MonoBehaviour
    {
        private const float SHUT_HEIGHT = 222;
        private const float OPEN_HEIGHT = 270;

        [SerializeField] private Button _button;
        [SerializeField] private Image _ribbon;
        [SerializeField] private Image _emblem;

        public event Action<string> Tapped;

        public string Tome { get; private set; }

        public void Show(BookmarkData mark)
        {
            Tome = mark.Tome;
            _emblem.sprite = mark.Emblem;
            _ribbon.color = mark.Open ? mark.Tint : mark.Tint * new Color(0.9f, 0.9f, 0.9f, 1f);
            var rect = (RectTransform)transform;
            rect.DOKill();
            rect.DOSizeDelta(new Vector2(rect.sizeDelta.x, mark.Open ? OPEN_HEIGHT : SHUT_HEIGHT), 0.16f);
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);

        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);

        private void OnTapped() => Tapped?.Invoke(Tome);
    }
}
