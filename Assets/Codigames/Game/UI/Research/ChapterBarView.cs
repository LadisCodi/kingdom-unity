using Codigames.Game.UI.Data.Research;
using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Research
{
    // A chapter heading across the page, and what is left to reveal while it is shut. View only.
    public class ChapterBarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _gate;

        public void Show(ChapterData chapter)
        {
            ((RectTransform)transform).anchoredPosition = new Vector2(0, -(chapter.Top + TechPageLayout.ROW_GAP / 2));
            _name.text = chapter.Name;
            _gate.text = chapter.Gate;
            _gate.gameObject.SetActive(!string.IsNullOrEmpty(chapter.Gate));
        }
    }
}
