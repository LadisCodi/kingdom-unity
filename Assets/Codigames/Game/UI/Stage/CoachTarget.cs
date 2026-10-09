using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Stage
{
    // A control a scene can name ("nav:build", "card:upgrade", "build:Housing"): what a line waits to see on screen,
    // and what its pointer shows. Views tag their controls with `Tag`, rows and cards when they are filled; UiTargets
    // finds the ones enabled.
    [RequireComponent(typeof(RectTransform))]
    public class CoachTarget : MonoBehaviour
    {
        [SerializeField] private List<string> _keys = new();

        public RectTransform Rect => (RectTransform)transform;

        // Names `control` by these keys, replacing any it had.
        public static void Tag(Component control, params string[] keys)
        {
            if (control == null) return;
            if (!control.TryGetComponent<CoachTarget>(out var target)) target = control.gameObject.AddComponent<CoachTarget>();
            target._keys.Clear();
            target._keys.AddRange(keys);
        }

        // Named by this key, or by one of its kind when it ends in ':'.
        public bool Answers(string key)
        {
            var anyOfKind = key.EndsWith(":");
            foreach (var own in _keys)
                if (anyOfKind ? own.StartsWith(key) : own == key) return true;
            return false;
        }
    }
}
