using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Bag
{
    // One coin of a choice chest (the web's bag-choice-plate): its icon over what it would give now; the picked one
    // wears the gold rim.
    public class ChoicePlate : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private Image _rim;
        [SerializeField] private Color _rimColor = new Color32(0xcf, 0xa8, 0x74, 0xff);
        [SerializeField] private Color _pickedColor = new Color32(0xf2, 0xb2, 0x33, 0xff);

        public event Action<string> Tapped;

        public string Coin { get; private set; }

        public void Show(ChoicePlateData plate)
        {
            Coin = plate.Coin;
            _icon.sprite = plate.Icon;
            _amount.text = plate.Amount;
            _rim.color = plate.Picked ? _pickedColor : _rimColor;
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);
        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);
        private void OnTapped() => Tapped?.Invoke(Coin);
    }
}
