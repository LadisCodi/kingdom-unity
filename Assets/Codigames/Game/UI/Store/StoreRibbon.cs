using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Store
{
    // A shelf's name on a red cloth ribbon (the web's stx-ribbon).
    public class StoreRibbon : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        public void Show(string label) => _label.text = label;
    }
}
