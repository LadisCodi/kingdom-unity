using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Kit
{
    // A panel with a header (the web's k-headpanel): a painted plank nailed across the top — red, blue, wood or green —
    // over a sheet of deckled paper. The title sits on the plank at the left in the plank's cream; the trail (a total, a
    // timer) is anchored to its right.
    public class HeadPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _trail;
        [SerializeField] private RectTransform _body;

        public RectTransform Body => _body;

        public string Title
        {
            set => _title.text = value;
        }

        public string Trail
        {
            set => _trail.text = value;
        }

        public Color TrailColor
        {
            set => _trail.color = value;
        }
    }
}
