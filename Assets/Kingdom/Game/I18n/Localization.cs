using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Kingdom.Game.I18n
{
    // English is the key: Tr("Build") returns "Build" in English and its translation in Spanish, or the
    // English when a line has none yet. "{name}" placeholders are filled from the arguments. A key may carry
    // a context before "::" ("verb::Build") to split two meanings of one English word.
    public class Localization
    {
        private const string CONTEXT_SEPARATOR = "::";
        private const char PLURAL_SEPARATOR = '|';

        private readonly Dictionary<string, string> _spanish = new();

        public Language Current { get; private set; } = Language.English;

        public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("en-US");

        public event Action Changed;

        public void SetLanguage(Language language)
        {
            Current = language;
            Culture = CultureInfo.GetCultureInfo(language == Language.Spanish ? "es-ES" : "en-US");
            Changed?.Invoke();
        }

        // Adds a catalogue of English → Spanish lines (one per source file).
        public void AddSpanish(IReadOnlyDictionary<string, string> lines)
        {
            foreach (var line in lines) _spanish[line.Key] = line.Value;
        }

        public void ClearSpanish() => _spanish.Clear();

        public string Tr(string english) => Translate(english);

        public string Tr(string english, params (string Name, object Value)[] args) => Fill(Translate(english), args);

        // A count picks its form: Trn(3, "{n} coin", "{n} coins"). Spanish forms live under "one|other".
        public string Trn(int count, string one, string other, params (string Name, object Value)[] args)
        {
            var form = count == 1 ? English(one) : English(other);

            if (Current == Language.Spanish && _spanish.TryGetValue(one + PLURAL_SEPARATOR + other, out var both))
            {
                var forms = both.Split(PLURAL_SEPARATOR);
                var spanish = count == 1 ? forms[0] : (forms.Length > 1 ? forms[1] : string.Empty);
                if (!string.IsNullOrEmpty(spanish)) form = spanish;
            }

            return Fill(form, args);
        }

        private string Translate(string key)
        {
            if (Current == Language.Spanish && _spanish.TryGetValue(key, out var spanish) && !string.IsNullOrEmpty(spanish))
                return spanish;

            return English(key);
        }

        private string English(string key)
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
