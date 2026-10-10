using System;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // One decision, asked in a centred window over the screen that raised it (the web's centred sheet, §6.6): a picture,
    // the question, a muted note, and Cancel beside the action. View only: the ConfirmMenuPresenter fills it.
    public class ConfirmMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Image _art;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private TMP_Text _note;
        [SerializeField] private KitButton _cancel;
        [SerializeField] private KitButton _ok;

        public event Action CloseTapped;
        public event Action OkTapped;

        protected override void InitializeInternal() => CoachTarget.Tag(_close, "close");

        public void Show(string title, Sprite art, string text, string note, string cancel, string ok)
        {
            _title.text = title;
            _art.gameObject.SetActive(art != null);
            _art.sprite = art;
            _text.text = text;
            _note.gameObject.SetActive(!string.IsNullOrEmpty(note));
            _note.text = note ?? string.Empty;
            _cancel.Label = cancel;
            _ok.Label = ok;
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _cancel.onClick.AddListener(OnClose);
            _ok.onClick.AddListener(OnOk);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _cancel.onClick.RemoveListener(OnClose);
            _ok.onClick.RemoveListener(OnOk);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnOk() => OkTapped?.Invoke();
    }
}
