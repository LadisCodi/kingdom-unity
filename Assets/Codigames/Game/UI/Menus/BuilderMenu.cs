using System;
using System.Collections.Generic;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Store;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // THE BUILDER SHEET (the web's builderSheet, §5.6): raised by a build or an upgrade every builder is too busy for.
    // There is no waiting line: the sheet shows the crew — a free builder offering the very job it was raised for, each
    // busy one with its job, the time left and a Gem Finish — and Hire in the next empty place. It never closes on its
    // own: a job that ends turns its row free in place. View only: the BuilderMenuPresenter fills it.
    public class BuilderMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _head;
        [SerializeField] private TMP_Text _note;
        [SerializeField] private RectTransform _rows;
        [SerializeField] private StoreWide _rowPrefab;
        [SerializeField] private TMP_Text _ceiling;
        [SerializeField] private RectTransform _window;
        [SerializeField] private RectTransform _content;

        private readonly List<StoreWide> _views = new();
        private string _shown;
        private float? _chrome;

        public event Action CloseTapped;
        public event Action<string> RowTapped;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            _close.onClick.AddListener(() => CloseTapped?.Invoke());
        }

        // Rebuilt when the rows' shape moves; their words are written in place.
        public void Show(string title, string head, string note, IReadOnlyList<(string Id, StoreWideData Data)> rows, string ceiling)
        {
            _title.text = title;
            _head.text = head;
            _note.text = note;
            _ceiling.gameObject.SetActive(!string.IsNullOrEmpty(ceiling));
            _ceiling.text = ceiling ?? string.Empty;
            var shape = string.Join(",", System.Linq.Enumerable.Select(rows, r => r.Id));
            if (shape != _shown)
            {
                _shown = shape;
                foreach (var view in _views) Destroy(view.gameObject);
                _views.Clear();
                foreach (var (id, _) in rows)
                {
                    var view = Instantiate(_rowPrefab, _rows);
                    var rowId = id;
                    view.Tapped += () => RowTapped?.Invoke(rowId);
                    _views.Add(view);
                }

                WireClicks(_rows.gameObject);
            }

            for (var i = 0; i < rows.Count; i++) _views[i].Show(rows[i].Data);
            Fit();
        }

        protected override void PreShowInternal() => Fit();

        private void Fit()
        {
            if (_window == null || _content == null || !isActiveAndEnabled) return;
            _chrome ??= _window.rect.height - ((RectTransform)_content.parent).rect.height;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _window.sizeDelta = new Vector2(_window.sizeDelta.x, LayoutUtility.GetPreferredHeight(_content) + _chrome.Value);
        }
    }
}
