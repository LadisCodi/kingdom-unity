using UnityEngine;

namespace Codigames.Game.Localization
{
    // The language the game speaks: the one chosen in the settings (kept per device), else the device's own,
    // when the game has it, else English.
    public static class DeviceLanguage
    {
        public const string CHOSEN_KEY = "kingdom.language";

        private const string ENGLISH = "en-US";
        private const string SPANISH = "es-ES";

        public static string Culture()
        {
            var chosen = PlayerPrefs.GetString(CHOSEN_KEY, string.Empty);
            if (chosen == ENGLISH || chosen == SPANISH) return chosen;

            return Application.systemLanguage == SystemLanguage.Spanish ? SPANISH : ENGLISH;
        }
    }
}
