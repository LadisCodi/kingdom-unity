using System;
using System.Collections.Generic;
using Codigames.Modules.Localization;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Localization
{
    // Every translation the game ships: per language, files of English source text → its translation (the
    // web's UI lines, its data overlays flattened, and the lines this build added).
    [CreateAssetMenu(fileName = "Localization", menuName = "Kingdom/Localization Catalog")]
    public class LocalizationCatalog : ScriptableObject
    {
        [SerializeField, ListDrawerSettings(ShowFoldout = false)] private List<Language> _languages = new();

        public void Fill(Localizer localizer)
        {
            foreach (var language in _languages)
            {
                foreach (var file in language.Files)
                {
                    if (file == null) continue;
                    localizer.AddTranslations(language.Code, JsonConvert.DeserializeObject<Dictionary<string, string>>(file.text));
                }
            }
        }

        [Serializable]
        private class Language
        {
            [SerializeField, Tooltip("Two letters: es.")] private string _code;
            [SerializeField] private List<TextAsset> _files = new();

            public string Code => _code;
            public IReadOnlyList<TextAsset> Files => _files;
        }
    }
}
