using System;
using System.Collections.Generic;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Widgets;
using TMPro;
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
        [Header("Note")]
        [SerializeField, Tooltip("The line over the rows; the list moves down while it shows.")] private GameObject _note;
        [SerializeField] private Image _noteIcon;
        [SerializeField] private TMP_Text _noteText;
        [SerializeField] private TMP_Text _noteSide;
        [SerializeField] private Image _noteRim;
        [SerializeField] private RectTransform _list;
        [SerializeField, Tooltip("The list's top inset without a note, and how much a note adds, in reference pixels.")] private float _listTop = 168;
        [SerializeField] private float _noteRoom = 92;
        [SerializeField] private Color _ink = new Color32(0x3b, 0x24, 0x12, 0xff);
        [SerializeField] private Color _muted = new Color32(0x7a, 0x5c, 0x3e, 0xff);
        [SerializeField] private Color _clay = new Color32(0xd4, 0x55, 0x3e, 0xff);
        [SerializeField] private Color _leaf = new Color32(0x3f, 0x8a, 0x2e, 0xff);
        [SerializeField] private Color _line = new Color32(0xcf, 0xa8, 0x74, 0xff);

        private readonly List<BuildRow> _shown = new();
        private readonly Dictionary<TabButton, Action> _tabHandlers = new();

        public event Action CloseTapped;

        // A tab was tapped: its id.
        public event Action<string> TabTapped;

        // A row was tapped: its building's id.
        public event Action<string> RowTapped;

        // What the tutorial's lines call its controls.
        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            foreach (var tab in _tabs) CoachTarget.Tag(tab, "build-tab:" + tab.Id);
        }

        public void SetOpenTab(string id)
        {
            foreach (var tab in _tabs) tab.SetOpen(tab.Id == id);
        }

        // Each tab's call to action: how many of its buildings can be built now.
        public void SetTabBadge(string id, int count)
        {
            foreach (var tab in _tabs)
                if (tab.Id == id) tab.SetBadge(count);
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

        // The line over the rows, or none.
        public void SetNote(BuildNoteData note)
        {
            _note.SetActive(note != null);
            _list.offsetMax = new Vector2(_list.offsetMax.x, -(_listTop + (note != null ? _noteRoom : 0)));
            if (note == null) return;

            var shortTone = note.Tone == BuildNoteTone.Short;
            _noteIcon.sprite = note.Icon;
            _noteText.text = note.Strong ? "<b>" + note.Text + "</b>" : note.Text;
            _noteText.color = shortTone ? _clay : _ink;
            _noteSide.gameObject.SetActive(!string.IsNullOrEmpty(note.Note));
            _noteSide.text = note.Tone == BuildNoteTone.Paying ? "<b>" + note.Note + "</b>" : note.Note;
            _noteSide.color = shortTone ? _clay : note.Tone == BuildNoteTone.Paying ? _leaf : _muted;
            _noteRim.color = shortTone ? _clay : _line;
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
