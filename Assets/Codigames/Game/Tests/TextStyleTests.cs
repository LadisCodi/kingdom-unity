using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Tests
{
    // Every label a menu prints names a role in the text style sheet (Assets/Settings/UI/Text Styles.asset),
    // never a size of its own, so one edit to the sheet retunes it everywhere. A role that sets a weight
    // sits on Nunito Regular: the weight table swaps the face, and Regular is the only base it can go down from.
    public class TextStyleTests
    {
        private const string UI_PREFABS = "Assets/Prefabs/UI";
        private const string SHEET = "Assets/Settings/UI/Text Styles.asset";
        private const string REGULAR = "Nunito-Regular SDF";
        private const string DISPLAY = "Alegreya-Black SDF";

        // Labels that size themselves: the header's coins and the bars set their size from code, the research
        // page is drawn in the web's pixels and scaled as a whole, and the crew's ± are glyphs on a button.
        private static readonly string[] EXEMPT =
        {
            "CurrencySlot/Amount",
            "HeaderMenu/KnowledgeTab/",
            "ProgressBar/Label",
            "Research/ChapterBar/",
            "Research/TechCard/",
            "Crew/Minus/Glyph",
            "Crew/Plus/Glyph",
        };

        // Roles whose label keeps its own face and material (a title's band, a slab button, a plank).
        private static readonly HashSet<string> DISPLAY_ROLES = new() { "Title", "Subtitle", "Heading" };
        private static readonly HashSet<string> MATERIAL_ROLES = new() { "Button", "Button Big", "Quest Button", "Tab", "Nav", "Toast" };

        [Test]
        public void TheSheet_ShouldBeTextMeshProsDefault()
        {
            Assert.That(TMP_Settings.defaultStyleSheet, Is.EqualTo(AssetDatabase.LoadAssetAtPath<TMP_StyleSheet>(SHEET)));
        }

        [Test]
        public void EveryMenuLabel_ShouldNameARole()
        {
            var bare = Labels().Where(l => l.Text.textStyle.name == "Normal" && !EXEMPT.Any(l.Path.Contains)).Select(l => l.Path);

            Assert.That(bare, Is.Empty);
        }

        [Test]
        public void ALabelsFace_ShouldSuitItsRole()
        {
            var wrong = Labels()
                .Where(l => l.Text.textStyle.name != "Normal" && !MATERIAL_ROLES.Contains(l.Text.textStyle.name))
                .Where(l => l.Text.font.name != (DISPLAY_ROLES.Contains(l.Text.textStyle.name) ? DISPLAY : REGULAR))
                .Select(l => $"{l.Path} ({l.Text.textStyle.name} on {l.Text.font.name})");

            Assert.That(wrong, Is.Empty);
        }

        [Test]
        public void TheGameScene_ShouldNotPinALabelsStyle()
        {
            var scene = System.IO.File.ReadAllText("Assets/Scenes/Game.unity");

            Assert.That(scene, Does.Not.Contain("propertyPath: m_TextStyleHashCode"));
        }

        private static IEnumerable<(string Path, TMP_Text Text)> Labels()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { UI_PREFABS }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                    yield return (path.Substring(UI_PREFABS.Length + 1).Replace(".prefab", "") + PathOf(text.transform, root.transform), text);
            }
        }

        private static string PathOf(Transform t, Transform root)
        {
            var path = "";
            for (; t != root; t = t.parent) path = "/" + t.name + path;
            return path;
        }
    }
}
