using Codigames.Game.UI.Menus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Store
{
    // What an offer opens for good (the web's ofs-gift): a parchment strip in its nailed frame under a green GIFT tag,
    // the picture on a reward tile, the title and what it does.
    public class OfferGiftView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _text;

        public void Show(OfferGiftData data)
        {
            _icon.sprite = data.Icon;
            _title.text = data.Title;
            _text.text = data.Text;
        }
    }
}
