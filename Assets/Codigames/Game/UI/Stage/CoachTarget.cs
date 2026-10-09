using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Stage
{
    // A control a scene can name ("nav:build", "card:upgrade", "build:Housing"): what a line waits to see on screen,
    // and what its pointer shows. Rows and cards built at runtime set their key when they are filled.
    [RequireComponent(typeof(RectTransform))]
    public class CoachTarget : MonoBehaviour
    {
        private static readonly List<CoachTarget> ACTIVE = new();

        [SerializeField] private string _key;

        public string Key => _key;

        public RectTransform Rect => (RectTransform)transform;

        // Every control enabled now, in no particular order.
        public static IReadOnlyList<CoachTarget> Active => ACTIVE;

        public void SetKey(string key) => _key = key;

        private void OnEnable() => ACTIVE.Add(this);

        private void OnDisable() => ACTIVE.Remove(this);
    }
}
