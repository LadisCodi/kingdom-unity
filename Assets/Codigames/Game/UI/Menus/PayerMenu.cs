using System;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Menus
{
    // "Who are you playing as?" (the web's payerSheet): modal in the strong sense — no close, nothing else opens until a
    // profile is picked. Why it asks, five profiles each with its budget and a line, and that the choice is final.
    public class PayerMenu : Menu
    {
        [Serializable]
        private class Option
        {
            public TMP_Text Name;
            public TMP_Text Budget;
            public TMP_Text Blurb;
            public KitButton Button;
        }

        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _lede;
        [SerializeField] private TMP_Text _fine;
        [SerializeField] private Option[] _options = Array.Empty<Option>();

        public event Action<int> Chosen;

        protected override void InitializeInternal()
        {
            for (var i = 0; i < _options.Length; i++)
            {
                var index = i;
                _options[i].Button.onClick.AddListener(() => Chosen?.Invoke(index));
            }
        }

        public void Show(string title, string lede, string fine, (string Name, string Budget, string Blurb, string Button)[] options)
        {
            _title.text = title;
            _lede.text = lede;
            _fine.text = "<sprite name=\"padlock\"> " + fine;
            for (var i = 0; i < _options.Length; i++)
            {
                _options[i].Name.text = options[i].Name;
                _options[i].Budget.text = options[i].Budget;
                _options[i].Blurb.text = options[i].Blurb;
                _options[i].Button.Label = options[i].Button;
            }
        }
    }
}
