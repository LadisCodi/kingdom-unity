using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Kit
{
    // A tag (the web's tr-tag): a short chip for what a unit is or does — sky for its type, paper for a trait.
    public class Tag : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        public string Label
        {
            set => _label.text = value;
        }
    }
}
