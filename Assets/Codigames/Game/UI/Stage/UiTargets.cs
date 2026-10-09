using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Stage
{
    // The controls on screen, by the key a scene names them with. A key ending in ':' is any of its kind
    // ("notice:" is any notice); "back" is the close of whatever is open on top.
    public class UiTargets
    {
        public const string BACK = "back";
        private const string CLOSE = "close";

        private CoachTarget[] _enabled = new CoachTarget[0];
        private int _foundAt = -1;

        // The tagged controls enabled now, looked up once a frame however often they are asked for.
        private CoachTarget[] Enabled
        {
            get
            {
                if (_foundAt == Time.frameCount) return _enabled;
                _foundAt = Time.frameCount;
                _enabled = Object.FindObjectsByType<CoachTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                return _enabled;
            }
        }

        // The control with this key that the player can see now, or null.
        public RectTransform Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key == BACK) return Topmost(CLOSE);

            foreach (var target in Enabled)
                if (target.Answers(key) && IsSeen(target.Rect)) return target.Rect;
            return null;
        }

        // Of the controls with this key on screen, the one drawn last: a sheet opened over a book comes after it.
        private RectTransform Topmost(string key)
        {
            RectTransform best = null;
            List<int> bestPath = null;
            foreach (var target in Enabled)
            {
                if (!target.Answers(key) || !IsSeen(target.Rect)) continue;
                var path = DrawOrder(target.transform);
                if (bestPath != null && Compare(path, bestPath) <= 0) continue;
                best = target.Rect;
                bestPath = path;
            }

            return best;
        }

        // Its canvas's order, then its place in the hierarchy, outermost first.
        private static List<int> DrawOrder(Transform transform)
        {
            var path = new List<int>();
            for (var t = transform; t != null; t = t.parent) path.Insert(0, t.GetSiblingIndex());
            var canvas = transform.GetComponentInParent<Canvas>();
            path.Insert(0, canvas != null ? canvas.sortingOrder : 0);
            return path;
        }

        private static int Compare(IReadOnlyList<int> a, IReadOnlyList<int> b)
        {
            for (var i = 0; i < Mathf.Min(a.Count, b.Count); i++)
                if (a[i] != b[i]) return a[i].CompareTo(b[i]);
            return a.Count.CompareTo(b.Count);
        }

        // On screen, not merely enabled: no faded group above it, and some size to it.
        private static bool IsSeen(RectTransform rect)
        {
            if (rect.rect.width <= 0 && rect.rect.height <= 0) return false;
            for (var t = rect.transform; t != null; t = t.parent)
                if (t.TryGetComponent<CanvasGroup>(out var group) && group.alpha <= 0.01f) return false;
            return true;
        }
    }
}
