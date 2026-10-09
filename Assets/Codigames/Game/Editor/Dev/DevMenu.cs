using Codigames.Game.Dev;
using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.Dev
{
    // The dev switches, from the editor's menu. They take effect at once, in play too.
    public static class DevMenu
    {
        private const string TUTORIALS_OFF = "Kingdom/Dev/Tutorials off";

        [MenuItem(TUTORIALS_OFF)]
        private static void ToggleTutorials()
        {
            var off = PlayerPrefs.GetInt(DevSwitches.TUTORIALS_OFF, 0) == 1;
            PlayerPrefs.SetInt(DevSwitches.TUTORIALS_OFF, off ? 0 : 1);
            PlayerPrefs.Save();
        }

        [MenuItem(TUTORIALS_OFF, true)]
        private static bool ShowTutorials()
        {
            Menu.SetChecked(TUTORIALS_OFF, PlayerPrefs.GetInt(DevSwitches.TUTORIALS_OFF, 0) == 1);
            return true;
        }
    }
}
