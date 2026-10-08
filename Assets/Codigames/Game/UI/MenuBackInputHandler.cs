using Codigames.Modules.UI;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Codigames.Game.UI
{
    // Closes the top-most menu when the player presses Escape on desktop or the Android back button (Unity
    // surfaces the Android back button as the keyboard Escape key): the UI module's back, from Unity's input.
    public class MenuBackInputHandler : ITickable
    {
        private readonly UIManager _ui;

        public MenuBackInputHandler(UIManager ui)
        {
            _ui = ui;
        }

        public void Tick()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) _ui.CloseTopMost();
        }
    }
}
