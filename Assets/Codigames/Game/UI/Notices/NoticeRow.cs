using System;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Notices
{
    // A line of a group's card (the web's nt-row): its picture, its name and line, and Go — or Open, on the +N's card.
    public class NoticeRow : MonoBehaviour
    {
        [SerializeField] private Image _art;
        [SerializeField] private TMP_Text _carved;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _line;
        [SerializeField] private KitButton _button;

        private NoticeRowData _row;

        public event Action<NoticeRowData> Tapped;

        public void Show(NoticeRowData row, string go, string open)
        {
            _row = row;
            NoticeArt.Fit(_art, row.Art, row.ArtIsBuilding);
            _carved.gameObject.SetActive(!string.IsNullOrEmpty(row.Carved));
            _carved.text = row.Carved;
            _name.text = row.Name;
            _line.gameObject.SetActive(!string.IsNullOrEmpty(row.Line));
            _line.text = row.Line;
            _button.gameObject.SetActive(row.Go != null || row.Opens != null);
            _button.Label = row.Opens != null ? open : go;
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);
        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);
        private void OnTapped() => Tapped?.Invoke(_row);
    }
}
