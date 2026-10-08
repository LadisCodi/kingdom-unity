
using UnityEngine;

namespace Kingdom.Game.Feedback
{
    // A short line shown as feedback for an action (a refused tap), and where it appears.
    public readonly struct QuickInfoMessageData
    {
        public string Message { get; }

        // Screen position where the message appears. Null means the center of the screen.
        public Vector2? ScreenPosition { get; }

        public QuickInfoMessageData(string message, Vector2? screenPosition = null)
        {
            Message = message;
            ScreenPosition = screenPosition;
        }
    }
}
