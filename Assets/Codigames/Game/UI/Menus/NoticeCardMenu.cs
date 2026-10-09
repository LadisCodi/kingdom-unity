using System;
using System.Collections.Generic;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Notices;
using Codigames.Game.UI.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // A notice's card (Docs/features/26-notices.md §5): a centred parchment window — one news with its paragraph, its
    // picture wide and Go; a group, a row each with its own Go; the +N, every notice as a row that opens its card.
    // View only: the NoticeCardMenuPresenter fills it.
    public class NoticeCardMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private GameObject _pictureFrame;
        [SerializeField] private Image _picture;
        [SerializeField] private GameObject _actions;
        [SerializeField] private KitButton _go;
        [SerializeField] private RectTransform _rows;
        [SerializeField] private NoticeRow _rowPrefab;
        [SerializeField] private RectTransform _window;

        private readonly List<NoticeRow> _rowViews = new();

        public event Action CloseTapped;
        public event Action GoTapped;
        public event Action<NoticeRowData> RowTapped;

        protected override void InitializeInternal() => CoachTarget.Tag(_close, "close");

        public void Show(Notice notice, string go, string rowGo, string open)
        {
            _title.text = notice.Title;
            _body.gameObject.SetActive(!string.IsNullOrEmpty(notice.Body));
            _body.text = notice.Body;
            _pictureFrame.SetActive(notice.Picture != null);
            if (notice.Picture != null) NoticeArt.Fit(_picture, notice.Picture, notice.ArtIsBuilding);
            _actions.SetActive(notice.Go != null);
            _go.Label = go;

            _rows.gameObject.SetActive(notice.Rows.Count > 0);
            for (var i = 0; i < notice.Rows.Count; i++)
            {
                if (i == _rowViews.Count)
                {
                    var row = Instantiate(_rowPrefab, _rows);
                    row.Tapped += r => RowTapped?.Invoke(r);
                    _rowViews.Add(row);
                }

                _rowViews[i].gameObject.SetActive(true);
                _rowViews[i].Show(notice.Rows[i], rowGo, open);
            }

            for (var i = notice.Rows.Count; i < _rowViews.Count; i++) _rowViews[i].gameObject.SetActive(false);
            LayoutRebuilder.MarkLayoutForRebuild(_window);
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _go.onClick.AddListener(OnGo);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _go.onClick.RemoveListener(OnGo);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnGo() => GoTapped?.Invoke();
    }
}
