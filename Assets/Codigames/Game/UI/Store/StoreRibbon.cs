using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Store
{
    // A shelf's name on a red cloth ribbon (the web's stx-ribbon), with the time left beside it when it has one.
    public class StoreRibbon : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private GameObject _timer;
        [SerializeField] private TMP_Text _countdown;

        public TMP_Text Countdown => _timer != null && _timer.activeSelf ? _countdown : null;

        public void Show(string label, bool timed = false)
        {
            _label.text = label;
            if (_timer != null) _timer.SetActive(timed);
        }
    }
}
