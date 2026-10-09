using System;
using Codigames.Game.UI.Data.Research;
using Codigames.Game.UI.Widgets;
using Codigames.Kingdom.Research;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Research
{
    // One technology on its page: the stat tile with its name, its emblem and the Knowledge bar, a tick once
    // researched or a padlock while locked. A locked card is the same card drained of colour; a full one glows;
    // the orb marks a press worth making now. View only.
    public class TechCardView : MonoBehaviour
    {
        private static readonly Color INK = new(0.231f, 0.141f, 0.071f);
        private static readonly Color LOCKED_INK = new(0.38f, 0.36f, 0.34f);

        [SerializeField] private Button _button;
        [SerializeField] private Graphic[] _drained;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private Image _icon;
        [SerializeField] private ProgressBar _bar;
        [SerializeField] private RectTransform _barRect;
        [SerializeField] private Image _mark;
        [SerializeField] private Sprite _tick;
        [SerializeField] private Sprite _padlock;
        [SerializeField] private GameObject _glow;
        [SerializeField] private GameObject _orb;
        [SerializeField] private Material _grayscale;
        [SerializeField] private CanvasGroup _group;

        public event Action<string> Tapped;

        public string Id { get; private set; }

        public void Show(TechCardData card)
        {
            Id = card.Id;
            var rect = (RectTransform)transform;
            rect.anchoredPosition = new Vector2(card.Position.x, -card.Position.y);

            _name.text = card.Name;
            _name.fontSize = card.Name.Length > 18 ? 11 : card.Name.Length > 15 ? 12 : 13;
            _icon.sprite = card.Icon;
            _bar.Set(card.Fraction, card.Bar, card.State == TechState.Done);

            var locked = card.State == TechState.Locked;
            foreach (var graphic in _drained) graphic.material = locked ? _grayscale : null;
            _name.color = locked ? LOCKED_INK : INK;
            _group.alpha = locked ? 0.8f : 1f;

            // The tick or the padlock takes the bar's end; in progress the bar runs the whole width.
            _mark.gameObject.SetActive(card.State != TechState.Progress);
            _barRect.offsetMax = new Vector2(card.State == TechState.Progress ? 0 : -21, _barRect.offsetMax.y);
            _mark.sprite = card.State == TechState.Done ? _tick : _padlock;
            _glow.SetActive(card.Ready);
            _orb.SetActive(card.Actionable);
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);

        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);

        private void OnTapped() => Tapped?.Invoke(Id);
    }
}
