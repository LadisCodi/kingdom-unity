using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace Codigames.Game.Tests
{
    // Every line the game's code says to the player has its Spanish: a literal passed to Tr is a key in one of
    // the Spanish files.
    public class LocalizationTests
    {
        private static readonly Regex TR = new("\\bTr\\(\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Compiled);

        [Test]
        public void EveryLineInCode_ShouldHaveItsSpanish()
        {
            var spanish = new HashSet<string>();
            foreach (var file in Directory.GetFiles(Path.Combine(Application.dataPath, "Localization", "es"), "*.json"))
                spanish.UnionWith(JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file)).Keys);

            var lines = Directory.GetFiles(Path.Combine(Application.dataPath, "Codigames", "Game"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains("Tests"))
                .SelectMany(path => TR.Matches(File.ReadAllText(path)).Cast<Match>().Select(m => Regex.Unescape(m.Groups[1].Value)))
                .Distinct()
                .ToList();
            var missing = lines.Where(line => !spanish.Contains(line)).ToList();

            Assert.That(lines, Has.Count.GreaterThan(10), "the scan found the game's lines");
            Assert.That(missing, Is.Empty, "Add these to Assets/Localization/es/unity.json:\n" + string.Join("\n", missing));
        }
    }
}
