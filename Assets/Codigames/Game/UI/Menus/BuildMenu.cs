using System;
using System.Collections.Generic;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The build menu: a window with three tabs and, under them, a row per building that scrolls. View only:
    // the BuildMenuPresenter fills it and decides what a tap does.
    public class BuildMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private List<TabButton> _tabs = new();
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _rows;
        [SerializeField] private BuildRow _rowPrefab;

        private readonly List<BuildRow> _shown = new();
        private readonly Dictionary<TabButton, Action> _tabHandlers = new();

        public event Action CloseTapped;

        // A tab was tapped: its id.
        public event Action<string> TabTapped;

        // A row was tapped: its building's id.
        public event Action<string> RowTapped;

        public void SetOpenTab(string id)
        {
            foreach (var tab in _tabs) tab.SetOpen(tab.Id == id);
        }

        public void SetRows(IReadOnlyList<BuildRowData> rows)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (i == _shown.Count)
                {
                    var row = Instantiate(_rowPrefab, _rows);
                    WireClicks(row.gameObject);
                    row.Tapped += () => RowTapped?.Invoke(row.Id);
                    _shown.Add(row);
                }

                _shown[i].gameObject.SetActive(true);
                _shown[i].Show(rows[i]);
            }

            for (var i = rows.Count; i < _shown.Count; i++) _shown[i].gameObject.SetActive(false);
        }

        public void ScrollToTop() => _scroll.verticalNormalizedPosition = 1;

        public void Shake(string id)
        {
            foreach (var row in _shown)
            {
                if (row.gameObject.activeSelf && row.Id == id) row.Shake();
            }
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            foreach (var tab in _tabs) tab.Tapped += OnTab(tab);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            foreach (var tab in _tabs) tab.Tapped -= OnTab(tab);
        }

        private void OnClose() => CloseTapped?.Invoke();

        private Action OnTab(TabButton tab)
        {
            if (!_tabHandlers.TryGetValue(tab, out var handler)) _tabHandlers[tab] = handler = () => TabTapped?.Invoke(tab.Id);
            return handler;
        }
    }
}
