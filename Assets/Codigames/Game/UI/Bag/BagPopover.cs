using System;
using System.Collections.Generic;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Bag
{
    // The picked item's popover, opened inside the grid under its row (the web's bag-pop): its name and line, a choice
    // chest's coins, the quantity — − the trough + · the number · Max — and the total, and its one button. The slider is
    // the view's own while it moves: each step asks the presenter for the total and the button's words, nothing else
    // is redrawn under the finger.
    public class BagPopover : MonoBehaviour
    {
        private const int COLUMNS = 4;

        [SerializeField] private RectTransform _notch;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _line;
        [SerializeField] private TMP_Text _note;
        [SerializeField] private RectTransform _choice;
        [SerializeField] private ChoicePlate _platePrefab;
        [SerializeField] private GameObject _quantityRow;
        [SerializeField] private KitButton _minus;
        [SerializeField] private Slider _slider;
        [SerializeField] private KitButton _plus;
        [SerializeField] private TMP_Text _quantity;
        [SerializeField] private KitButton _max;
        [SerializeField] private TMP_Text _total;
        [SerializeField] private KitButton _action;

        private readonly List<ChoicePlate> _plates = new();
        private readonly Vector3[] _corners = new Vector3[4];
        private bool _showing;

        public event Action<int> QuantityChanged;
        public event Action<string> CoinPicked;
        public event Action ActionTapped;

        public void Show(BagPopoverData pop)
        {
            _showing = true;
            _name.text = pop.Name;
            _line.text = pop.Line;
            _note.gameObject.SetActive(!string.IsNullOrEmpty(pop.Note));
            _note.text = pop.Note;

            var choice = pop.Choice ?? Array.Empty<ChoicePlateData>();
            _choice.gameObject.SetActive(choice.Count > 0);
            for (var i = 0; i < choice.Count; i++)
            {
                if (i == _plates.Count)
                {
                    var plate = Instantiate(_platePrefab, _choice);
                    plate.Tapped += coin => CoinPicked?.Invoke(coin);
                    _plates.Add(plate);
                }

                _plates[i].gameObject.SetActive(true);
                _plates[i].Show(choice[i]);
            }

            for (var i = choice.Count; i < _plates.Count; i++) _plates[i].gameObject.SetActive(false);

            _quantityRow.SetActive(pop.Max > 1);
            _total.gameObject.SetActive(pop.Max > 1 && !string.IsNullOrEmpty(pop.Total));
            _slider.minValue = 1;
            _slider.maxValue = Mathf.Max(1, pop.Max);
            _slider.SetValueWithoutNotify(pop.Quantity);
            _quantity.text = pop.QuantityText;
            _total.text = pop.Total;

            _action.gameObject.SetActive(pop.Action != BagAction.None);
            _action.Label = pop.ActionLabel;
            _notch.anchorMin = _notch.anchorMax = new Vector2((pop.Column + 0.5f) / COLUMNS, 1);
            _showing = false;
        }

        // The total and the button's words for the quantity on the slider.
        public void ShowQuantity(int quantity, string total, string action, string shown)
        {
            _quantity.text = shown;
            _total.text = total;
            _action.Label = action;
        }

        // Where the reward flies from: the button's middle, on the screen.
        public Vector2 ActionScreenPoint()
        {
            ((RectTransform)_action.transform).GetWorldCorners(_corners);
            return RectTransformUtility.WorldToScreenPoint(null, (_corners[0] + _corners[2]) / 2f);
        }

        private void OnEnable()
        {
            _slider.onValueChanged.AddListener(OnSlider);
            _minus.onClick.AddListener(OnMinus);
            _plus.onClick.AddListener(OnPlus);
            _max.onClick.AddListener(OnMax);
            _action.onClick.AddListener(OnAction);
        }

        private void OnDisable()
        {
            _slider.onValueChanged.RemoveListener(OnSlider);
            _minus.onClick.RemoveListener(OnMinus);
            _plus.onClick.RemoveListener(OnPlus);
            _max.onClick.RemoveListener(OnMax);
            _action.onClick.RemoveListener(OnAction);
        }

        private void OnSlider(float value)
        {
            if (!_showing) QuantityChanged?.Invoke(Mathf.RoundToInt(value));
        }

        private void OnMinus() => _slider.value = Mathf.Max(_slider.minValue, _slider.value - 1);
        private void OnPlus() => _slider.value = Mathf.Min(_slider.maxValue, _slider.value + 1);
        private void OnMax() => _slider.value = _slider.maxValue;
        private void OnAction() => ActionTapped?.Invoke();
    }
}
