using Codigames.Game.Feedback;
using TMPro;
using UnityEngine;

namespace Codigames.Game.Harvest
{
    // What a tap took, rising off the cell: the amount and the currency's icon. How it rises and fades is its
    // MMF_Player.
    public class YieldFeedbackView : WorldFeedbackView
    {
        [SerializeField] private SpriteRenderer _icon;
        [SerializeField] private TMP_Text _amount;

        public void Show(Sprite icon, string amount)
        {
            _icon.sprite = icon;
            _icon.color = Color.white;
            _amount.text = amount;
            _amount.alpha = 1f;
        }
    }
}
