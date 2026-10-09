using System;
using System.Collections.Generic;
using Codigames.Game.UI.Notices;
using UnityEngine;

namespace Codigames.Game.UI.Menus
{
    // The news column (Docs/features/26-notices.md §3): round bubbles at the bottom right, growing upward, newest
    // nearest the bottom and the +N on top. Persistent; hidden while a sheet or a card covers the map. View only:
    // the NoticesColumnPresenter fills it.
    public class NoticesColumn : Menu
    {
        [SerializeField] private RectTransform _column;
        [SerializeField] private NoticeBubble _bubblePrefab;
        [SerializeField, Tooltip("A bubble's side; 0 keeps the prefab's.")] private float _bubbleSize;

        private readonly List<NoticeBubble> _bubbles = new();

        public event Action<string> BubbleTapped;

        // Draws the bubbles in order, top first; the ones whose ids are in `arrived` pop in.
        public void Show(IReadOnlyList<Notice> notices, ICollection<string> arrived)
        {
            for (var i = 0; i < notices.Count; i++)
            {
                if (i == _bubbles.Count)
                {
                    var bubble = Instantiate(_bubblePrefab, _column);
                    if (_bubbleSize > 0)
                    {
                        var layout = bubble.GetComponent<UnityEngine.UI.LayoutElement>();
                        layout.minWidth = layout.preferredWidth = layout.minHeight = layout.preferredHeight = _bubbleSize;
                    }

                    bubble.Tapped += id => BubbleTapped?.Invoke(id);
                    _bubbles.Add(bubble);
                }

                _bubbles[i].gameObject.SetActive(true);
                _bubbles[i].Show(notices[i]);
                if (arrived.Contains(notices[i].Id)) _bubbles[i].Pop();
            }

            for (var i = notices.Count; i < _bubbles.Count; i++) _bubbles[i].gameObject.SetActive(false);
        }

        public void SetLeaving(string id, bool leaving)
        {
            foreach (var bubble in _bubbles)
                if (bubble.gameObject.activeSelf && bubble.Id == id) bubble.SetLeaving(leaving);
        }

        public void SetClock(string id, string text)
        {
            foreach (var bubble in _bubbles)
                if (bubble.gameObject.activeSelf && bubble.Id == id) bubble.SetClock(text);
        }

        public void SetHidden(bool hidden)
        {
            CanvasGroup.alpha = hidden ? 0 : 1;
            CanvasGroup.blocksRaycasts = !hidden;
        }
    }
}
