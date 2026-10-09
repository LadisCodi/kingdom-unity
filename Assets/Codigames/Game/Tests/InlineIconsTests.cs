using System.Linq;
using Codigames.Game.Data.Economy;
using Codigames.Game.Editor.Art;
using NUnit.Framework;
using TMPro;
using UnityEditor;

namespace Codigames.Game.Tests
{
    // A price writes <sprite name="<currency>">: every currency, and every glyph the UI writes, has its sprite in
    // TextMeshPro's default sprite asset. A miss means Kingdom › UI › Rebuild inline icons.
    public class InlineIconsTests
    {
        [Test]
        public void TheDefaultSpriteAsset_ShouldBeTheInlineIcons()
        {
            Assert.That(AssetDatabase.GetAssetPath(TMP_Settings.defaultSpriteAsset), Is.EqualTo(InlineIconsBuilder.ASSET));
        }

        [Test]
        public void EveryCurrencyAndGlyph_ShouldHaveItsSprite()
        {
            var asset = TMP_Settings.defaultSpriteAsset;
            var missing = InlineIconsBuilder.Sources().Select(s => s.Name).Where(n => asset.GetSpriteIndexFromName(n) < 0);

            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void EveryCurrency_ShouldHaveAnIcon()
        {
            var bare = AssetDatabase.FindAssets("t:" + nameof(CurrencyCollection))
                .Select(g => AssetDatabase.LoadAssetAtPath<CurrencyCollection>(AssetDatabase.GUIDToAssetPath(g)))
                .SelectMany(c => c.Entries.OfType<CurrencyAsset>())
                .Where(c => c.Icon == null)
                .Select(c => c.Id);

            Assert.That(bare, Is.Empty);
        }
    }
}
