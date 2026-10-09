using System;
using System.Collections.Generic;
using Codigames.Game.UI.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Widgets
{
    // One building in the build menu: its art, its name and number, its promise and price, and at the right
    // the wait and how many stand. View only: the build menu's presenter fills it.
    public class BuildRow : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _art;
        [SerializeField, Tooltip("Sizes the art to the frame's width, feet on its floor.")] private AspectRatioFitter _artFit;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _promise;
        [SerializeField] private RectTransform _price;
        [SerializeField] private CostChip _chipPrefab;
        [SerializeField] private TMP_Text _wait;
        [SerializeField] private TMP_Text _built;
        [SerializeField] private GameObject _padlock;
        [SerializeField, Tooltip("The wait and the count: a row a technology has still to open has neither yet.")] private GameObject _side;
        [SerializeField] private Color _fillColor = new Color32(0xe2, 0xcc, 0xa0, 0xff);
        [SerializeField] private Color _unavailableColor = new Color32(0xcb, 0xba, 0x96, 0xff);
        [SerializeField] private Color _ordinalColor = new Color32(0x7a, 0x5c, 0x3e, 0xff);

        private readonly List<CostChip> _chips = new();
        private Tween _shake;

        public event Action Tapped;

        public string Id { get; private set; }

        public void Show(BuildRowData row)
        {
            Id = row.Id;
            _art.sprite = row.Art;
            _art.enabled = row.Art != null;
            if (row.Art != null) _artFit.aspectRatio = row.Art.rect.width / row.Art.rect.height;
            _name.text = string.IsNullOrEmpty(row.Ordinal)
                ? row.Name
                : $"{row.Name}<size=75%><color=#{ColorUtility.ToHtmlStringRGB(_ordinalColor)}> {row.Ordinal}</color></size>";
            _promise.text = row.Promise;
            _wait.text = row.Wait;
            _built.text = row.Built;
            _fill.color = row.Available ? _fillColor : _unavailableColor;
            if (_padlock != null) _padlock.SetActive(row.Locked);
            if (_side != null) _side.SetActive(!row.Locked);

            for (var i = 0; i < row.Price.Count; i++)
            {
                if (i == _chips.Count) _chips.Add(Instantiate(_chipPrefab, _price));
                _chips[i].gameObject.SetActive(true);
                _chips[i].Show(row.Price[i]);
            }

            for (var i = row.Price.Count; i < _chips.Count; i++) _chips[i].gameObject.SetActive(false);
        }

        // A refused tap: the row shakes, as the web's does.
        public void Shake()
        {
            _shake?.Complete();
            _shake = transform.DOPunchPosition(new Vector3(14, 0, 0), 0.36f, 8, 0.5f);
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnTapped);
            _shake?.Complete();
        }

        private void OnTapped() => Tapped?.Invoke();
    }
}
