using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Widgets
{
    // A reward, loot and not a sentence: its icon and how much.
    public class RewardChip : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amount;

        public void Show(Sprite icon, string amount)
        {
            _icon.sprite = icon;
            _amount.text = amount;
        }
    }
}
