using UnityEngine;

namespace Codigames.Game.Dev
{
    // Switches for whoever works on the game, kept on this device only (never in a save) and read only in the editor
    // and development builds: a release build always plays as a player would.
    public class DevSwitches
    {
        public const string TUTORIALS_OFF = "kingdom.dev.tutorialsOff";
        public const string WARP = "kingdom.dev.warpMs";

        // How far the clock has been pushed forward on this device: kept, so a save made ahead of time is not frozen
        // until the real clock catches up with it. Zero in a release build.
        public static long Warp => Debug.isDebugBuild && long.TryParse(PlayerPrefs.GetString(WARP, "0"), out var ms) ? ms : 0;

        public static void KeepWarp(long ms)
        {
            PlayerPrefs.SetString(WARP, ms.ToString(System.Globalization.CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        // No scene, pointer or peek: the screen left clear (for screenshots, or to test a menu in peace).
        public bool TutorialsOff => Debug.isDebugBuild && PlayerPrefs.GetInt(TUTORIALS_OFF, 0) == 1;
    }
}
