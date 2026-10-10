using Codigames.Game.Dev;
using Codigames.Game.Startup;
using Codigames.Modules.Clock;
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

        // The web's time-warp: the clock pushed forward, so an absence plays out at once.
        [MenuItem("Kingdom/Dev/Skip 5 min")] private static void Skip5() => Skip(5 * 60_000L);
        [MenuItem("Kingdom/Dev/Skip 1 h")] private static void Skip60() => Skip(60 * 60_000L);
        [MenuItem("Kingdom/Dev/Skip 6 h")] private static void Skip360() => Skip(6 * 60 * 60_000L);
        [MenuItem("Kingdom/Dev/Skip 5 min", true)] private static bool CanSkip5() => Application.isPlaying;
        [MenuItem("Kingdom/Dev/Skip 1 h", true)] private static bool CanSkip60() => Application.isPlaying;
        [MenuItem("Kingdom/Dev/Skip 6 h", true)] private static bool CanSkip360() => Application.isPlaying;

        private static void Skip(long ms)
        {
            var scope = Object.FindFirstObjectByType<ProjectLifetimeScope>();
            if (scope == null) return;
            var clock = (WarpClock)scope.Container.Resolve(typeof(WarpClock));
            clock.Skip(ms);
            DevSwitches.KeepWarp(clock.OffsetMs);
        }

        [MenuItem(TUTORIALS_OFF, true)]
        private static bool ShowTutorials()
        {
            Menu.SetChecked(TUTORIALS_OFF, PlayerPrefs.GetInt(DevSwitches.TUTORIALS_OFF, 0) == 1);
            return true;
        }
    }
}
