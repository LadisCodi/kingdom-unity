using UnityEngine;

namespace Codigames.Game.UI.Stage
{
    // The controls on screen, by the key a scene names them with. A key ending in ':' is any of its kind
    // ("notice:" is any notice).
    public class UiTargets
    {
        // The control with this key that the player can see now, or null.
        public RectTransform Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var anyOfKind = key.EndsWith(":");
            foreach (var target in CoachTarget.Active)
            {
                var named = anyOfKind ? target.Key != null && target.Key.StartsWith(key) : target.Key == key;
                if (named && IsSeen(target.Rect)) return target.Rect;
            }

            return null;
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
