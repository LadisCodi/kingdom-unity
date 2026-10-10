using System;
using Codigames.Game.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Store
{
    // What one tab in a splash's row of offers shows.
    public struct OfferTabData
    {
        public Sprite Icon;
        public Sprite Bust;
        public string Kind;
        public bool Open;
        public bool Ready;
    }

    // One offer in the row along a splash's top (the web's ofs-tab): a small wooden square with its icon — the one on
    // screen raised in parchment and lit gold, one with something to claim marked with the call to action.
    public class OfferTab : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _frame;
        [SerializeField] private Sprite _wood;
        [SerializeField] private Sprite _parchment;
        [SerializeField] private GameObject _glow;
        [SerializeField] private OfferIconView _icon;
        [SerializeField] private CtaBadge _badge;

        public event Action Tapped;

        private void Awake() => _button.onClick.AddListener(() => Tapped?.Invoke());

        public void Show(OfferTabData data)
        {
            _icon.Show(data.Icon, data.Bust, data.Kind);
            _frame.sprite = data.Open ? _parchment : _wood;
            _glow.SetActive(data.Open);
            _badge.Show(data.Ready ? 1 : 0);
        }
    }
}
