using System;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Store
{
    // An offer's icon (the web's offerIcon): its own art, or — until it has some — on the gold reward tile, its hero's
    // bust standing at the tile's foot or a picture of what it is for. The map's widget and the splash's row of offers
    // both draw it.
    public class OfferIconView : MonoBehaviour
    {
        [Serializable]
        private struct KindIcon
        {
            public string Kind;
            public Sprite Icon;
        }

        [SerializeField] private Image _icon;
        [SerializeField] private GameObject _tile;
        [SerializeField] private Image _bust;
        [SerializeField] private Image _kind;
        [SerializeField] private KindIcon[] _kinds;

        public void Show(Sprite icon, Sprite bust, string kind)
        {
            var own = icon != null;
            _icon.gameObject.SetActive(own);
            _tile.SetActive(!own);
            if (own)
            {
                _icon.sprite = icon;
                return;
            }

            _bust.gameObject.SetActive(bust != null);
            _kind.gameObject.SetActive(bust == null);
            if (bust != null) _bust.sprite = bust;
            else _kind.sprite = Array.Find(_kinds, k => k.Kind == kind).Icon;
        }
    }
}
