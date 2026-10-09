using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Lairs
{
    // One tile of a lair's reward: the icon, the amount, and a word on its corner when the lair carries all it can.
    public class LairChip : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private GameObject _tag;
        [SerializeField] private TMP_Text _tagText;

        public void Show(Sprite icon, string value, string tag)
        {
            _icon.sprite = icon;
            _value.text = value;
            _tag.SetActive(!string.IsNullOrEmpty(tag));
            _tagText.text = tag ?? string.Empty;
        }
    }
}
