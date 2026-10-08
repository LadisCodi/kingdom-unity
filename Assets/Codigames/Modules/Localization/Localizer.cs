using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Codigames.Modules.Localization
{
    // The source language's text is the key: Tr("Build") returns "Build" in the source language and its
    // translation in any other, or the source text when a line has none yet. "{name}" placeholders are
    // filled from the arguments. A key may carry a context before "::" ("verb::Build") to split two
    // meanings of one source word. Plurals are stored as "one|other".
    public class Localizer
    {
        private const string CONTEXT_SEPARATOR = "::";
        private const char PLURAL_SEPARATOR = '|';

        private readonly Dictionary<string, Dictionary<string, string>> _translations = new();
        private readonly string _sourceCulture;

        public Localizer(string sourceCulture = "en-US")
        {
            _sourceCulture = sourceCulture;
            Culture = CultureInfo.GetCultureInfo(sourceCulture);
        }

        public CultureInfo Culture { get; private set; }

        public bool IsSource => Culture.Name == _sourceCulture;

        public event Action Changed;

        public void SetCulture(string cultureName)
        {
            Culture = CultureInfo.GetCultureInfo(cultureName);
            Changed?.Invoke();
        }

        // Adds lines (source text → translation) for a language, by its two-letter code ("es").
        public void AddTranslations(string language, IReadOnlyDictionary<string, string> lines)
        {
            if (!_translations.TryGetValue(language, out var table)) _translations[language] = table = new Dictionary<string, string>();
            foreach (var line in lines) table[line.Key] = line.Value;
        }

        public string Tr(string source) => Translate(source);

        public string Tr(string source, params (string Name, object Value)[] args) => Fill(Translate(source), args);

        // A count picks its form: Trn(3, "{n} coin", "{n} coins").
        public string Trn(int count, string one, string other, params (string Name, object Value)[] args)
        {
            var form = count == 1 ? WithoutContext(one) : WithoutContext(other);

            if (TryGetLine(one + PLURAL_SEPARATOR + other, out var both))
            {
                var forms = both.Split(PLURAL_SEPARATOR);
                var translated = count == 1 ? forms[0] : (forms.Length > 1 ? forms[1] : string.Empty);
                if (!string.IsNullOrEmpty(translated)) form = translated;
            }

            return Fill(form, args);
        }

        // A text that heads its line starts with a capital, in the player's culture: "crop plots" → "Crop plots".
        public string Capitalized(string text)
            => string.IsNullOrEmpty(text) ? text : Culture.TextInfo.ToUpper(text[0]) + text.Substring(1);

        private string Translate(string key) => TryGetLine(key, out var line) ? line : WithoutContext(key);

        private bool TryGetLine(string key, out string line)
        {
            line = null;
            if (IsSource) return false;
            return _translations.TryGetValue(Culture.TwoLetterISOLanguageName, out var table)
                && table.TryGetValue(key, out line) && !string.IsNullOrEmpty(line);
        }

        private static string WithoutContext(string key)
        {
            var at = key.LastIndexOf(CONTEXT_SEPARATOR, StringComparison.Ordinal);
            return at < 0 ? key : key.Substring(at + CONTEXT_SEPARATOR.Length);
        }

        private string Fill(string text, (string Name, object Value)[] args)
        {
            if (args == null || args.Length == 0 || text.IndexOf('{') < 0) return text;

            var result = new StringBuilder(text);

            foreach (var (name, value) in args)
            {
                var shown = value is IFormattable formattable ? formattable.ToString(null, Culture) : value?.ToString();
                result.Replace("{" + name + "}", shown);
            }

            return result.ToString();
        }
    }
}
