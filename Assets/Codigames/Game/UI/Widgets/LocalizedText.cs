using Codigames.Modules.Localization;
using TMPro;
using UnityEngine;
using VContainer;

namespace Codigames.Game.UI.Widgets
{
    // A label written in a prefab: the text authored on it is the English source, shown in the player's language.
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        private TMP_Text _text;
        private string _source;
        private Localizer _localizer;

        [Inject]
        public void Construct(Localizer localizer)
        {
            _localizer = localizer;
            if (isActiveAndEnabled) OnEnable();
        }

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _source = _text.text;
        }

        private void OnEnable()
        {
            if (_localizer == null) return;
            _localizer.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            if (_localizer != null) _localizer.Changed -= Apply;
        }

        private void Apply() => _text.text = _localizer.Tr(_source);
    }
}
