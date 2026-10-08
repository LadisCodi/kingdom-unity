using Codigames.Modules.Feedback;
using Codigames.Modules.UI;
using UnityEngine;

namespace Codigames.Game.UI
{
    // Quick-info messages are drawn on the game's UI root, above the menus.
    public class QuickInfoMessageLayer : IQuickInfoMessageLayer
    {
        private readonly UIRoot _root;

        public QuickInfoMessageLayer(UIRoot root)
        {
            _root = root;
        }

        public RectTransform Container => _root.Container;
    }
}
