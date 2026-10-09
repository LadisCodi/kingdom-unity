using System;
using System.Collections.Generic;
using Codigames.Game.UI.Bag;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The speed-up picker (the web's speedupSheet): opened by a timer's Speed up and by the Bag. The job at the top,
    // its time left large; Auto over the rows; one row per speed-up that fits; Finish with its Gems last, never first.
    // Each Use shortens the time in place. View only: the SpeedupMenuPresenter fills it.
    public class SpeedupMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private ProgressBar _bar;
        [SerializeField] private TMP_Text _left;
        [SerializeField] private GameObject _autoRow;
        [SerializeField] private KitButton _auto;
        [SerializeField] private RectTransform _rows;
        [SerializeField] private SpeedupRow _rowPrefab;
        [SerializeField, Tooltip("The rows' scrolling area, sized to them up to the height below.")] private LayoutElement _rowsArea;
        [SerializeField, Tooltip("The tallest the rows grow before they scroll, in reference pixels.")] private float _rowsMaxHeight = 1000;
        [SerializeField] private TMP_Text _none;
        [SerializeField] private CostButton _finish;
        [SerializeField] private RectTransform _window;

        private readonly List<SpeedupRow> _rowViews = new();

        public event Action CloseTapped;
        public event Action AutoTapped;
        public event Action<string> UseTapped;
        public event Action FinishTapped;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            CoachTarget.Tag(_finish.Button, "speedup-finish");
        }

        public void Show(SpeedupScreenData screen)
        {
            _icon.sprite = screen.Icon;
            _title.text = screen.Title;
            _bar.Set(screen.Progress, string.Empty);
            _left.text = screen.Left;
            _autoRow.SetActive(!string.IsNullOrEmpty(screen.Auto));
            _auto.Label = screen.Auto;

            _none.gameObject.SetActive(screen.Rows.Count == 0);
            _none.text = screen.None;
            for (var i = 0; i < screen.Rows.Count; i++)
            {
                if (i == _rowViews.Count)
                {
                    var row = Instantiate(_rowPrefab, _rows);
                    row.UseTapped += id => UseTapped?.Invoke(id);
                    CoachTarget.Tag(row, "speedup-use");
                    _rowViews.Add(row);
                }

                _rowViews[i].gameObject.SetActive(true);
                _rowViews[i].Show(screen.Rows[i], screen.Use);
            }

            for (var i = screen.Rows.Count; i < _rowViews.Count; i++) _rowViews[i].gameObject.SetActive(false);
            // The rows scroll once they would grow the sheet past its height.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rows);
            _rowsArea.preferredHeight = Mathf.Min(LayoutUtility.GetPreferredHeight(_rows), _rowsMaxHeight);
            _rowsArea.gameObject.SetActive(screen.Rows.Count > 0);
            _finish.Show(screen.Finish, screen.CanFinish);
            LayoutRebuilder.MarkLayoutForRebuild(_window);
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _auto.onClick.AddListener(OnAuto);
            _finish.Button.onClick.AddListener(OnFinish);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _auto.onClick.RemoveListener(OnAuto);
            _finish.Button.onClick.RemoveListener(OnFinish);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnAuto() => AutoTapped?.Invoke();
        private void OnFinish() => FinishTapped?.Invoke();
    }
}
