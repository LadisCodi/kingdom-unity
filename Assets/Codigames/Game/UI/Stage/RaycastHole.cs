using UnityEngine;

namespace Codigames.Game.UI.Stage
{
    // A hole in a graphic that takes taps: a press inside it falls through to whatever is drawn below. The stage's
    // catcher holds everything back but the control a line asks the player to press.
    public class RaycastHole : MonoBehaviour, ICanvasRaycastFilter
    {
        // In screen pixels; null for no hole.
        public Rect? Hole { get; set; }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) => Hole == null || !Hole.Value.Contains(screenPoint);
    }
}
