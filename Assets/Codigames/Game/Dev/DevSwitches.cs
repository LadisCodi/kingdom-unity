using UnityEngine;

namespace Codigames.Game.Dev
{
    // Switches for whoever works on the game, kept on this device only (never in a save) and read only in the editor
    // and development builds: a release build always plays as a player would.
    public class DevSwitches
    {
        public const string TUTORIALS_OFF = "kingdom.dev.tutorialsOff";

        // No scene, pointer or peek: the screen left clear (for screenshots, or to test a menu in peace).
        public bool TutorialsOff => Debug.isDebugBuild && PlayerPrefs.GetInt(TUTORIALS_OFF, 0) == 1;
    }
}
