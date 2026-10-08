using UnityEngine;

namespace Codigames.Modules.Feedback
{
    // Port: the UI container quick-info messages are drawn in, above the menus.
    public interface IQuickInfoMessageLayer
    {
        RectTransform Container { get; }
    }
}
