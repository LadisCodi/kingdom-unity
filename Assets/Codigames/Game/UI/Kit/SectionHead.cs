using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Kit
{
    // A section's heading (the web's k-section-head): its name in small display capitals between two rules — a
    // short one before, the rest of the row after.
    public class SectionHead : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;

        public string Title
        {
            set => _title.text = value;
        }
    }
}
