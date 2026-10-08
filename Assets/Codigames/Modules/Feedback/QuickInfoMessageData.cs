using Codigames.Modules.Core;

namespace Codigames.Modules.Feedback
{
    // A short line shown as feedback for an action (a refused tap), and where it appears.
    public readonly struct QuickInfoMessageData
    {
        public QuickInfoMessageData(string message, Vector2? screenPosition = null)
        {
            Message = message;
            ScreenPosition = screenPosition;
        }

        public string Message { get; }

        // Screen position where it appears; null = the centre of the screen.
        public Vector2? ScreenPosition { get; }
    }
}
