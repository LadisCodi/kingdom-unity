using System;
using System.Collections.Generic;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Store
{
    // An offer's banner: its figure, its name on a red plank over the frame's top edge, its pitch, what it hands over,
    // the time it has left and its price; its value on a seal when it is worth more than a Gem pack.
    public sealed class StoreOfferData
    {
        public string Name;
        public string Pitch;
        public Sprite Figure;
        public bool HeroFigure;
        public IReadOnlyList<OfferTileData> Tiles = Array.Empty<OfferTileData>();
        public string More;
        public double? ClosesAt;
        public string Price;
        public string Value;
    }

    public class StoreOffer : MonoBehaviour
    {
        [SerializeField] private Image _figure;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _pitch;
        [SerializeField] private RectTransform _tiles;
        [SerializeField] private OfferTileView _tilePrefab;
        [SerializeField] private TMP_Text _more;
        [SerializeField] private GameObject _timer;
        [SerializeField] private TMP_Text _countdown;
        [SerializeField] private KitButton _buy;
        [SerializeField] private Button _banner;
        [SerializeField] private TMP_Text _value;

        public event Action Tapped;

        // The countdown, for the page to write each second; null without one.
        public TMP_Text Countdown => _timer.activeSelf ? _countdown : null;

        private void Awake()
        {
            _buy.onClick.AddListener(() => Tapped?.Invoke());
            _banner.onClick.AddListener(() => Tapped?.Invoke());
        }

        public void Show(StoreOfferData data)
        {
            _figure.sprite = data.Figure;
            _figure.enabled = data.Figure != null;
            _figure.rectTransform.localScale = Vector3.one * (data.HeroFigure ? 1.18f : 1f);
            _name.text = data.Name;
            _pitch.text = data.Pitch;
            for (var i = _tiles.childCount - 1; i >= 0; i--)
                if (_tiles.GetChild(i) != _more.transform.parent) Destroy(_tiles.GetChild(i).gameObject);
            foreach (var tile in data.Tiles) Instantiate(_tilePrefab, _tiles).Show(tile);
            _more.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(data.More));
            _more.text = data.More ?? string.Empty;
            _more.transform.parent.SetAsLastSibling();
            _timer.SetActive(data.ClosesAt != null);
            _buy.Label = data.Price;
            _value.gameObject.SetActive(!string.IsNullOrEmpty(data.Value));
            _value.text = data.Value ?? string.Empty;
        }
    }
}
