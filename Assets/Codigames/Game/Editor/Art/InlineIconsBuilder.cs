using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codigames.Game.Data.Economy;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace Codigames.Game.Editor.Art
{
    // Packs the icons a text may carry inline — every currency's, by its id, and the UI's own glyphs — into one
    // sheet and a TextMeshPro sprite asset, TMP's default, so any label can write <sprite name="Gold">.
    // A glyph is sized for a 48-rpx figure (the Price role): 76 rpx tall, the header coin, centred on the
    // digits, and it grows with the label's size. Rebuild after adding a currency (Kingdom › UI).
    public static class InlineIconsBuilder
    {
        public const string SHEET = "Assets/Art/UI/InlineIcons/inline-icons.png";
        public const string ASSET = "Assets/Art/UI/InlineIcons/Inline Icons.asset";
        private const string ICONS = "Assets/Art/UI/Icons/";
        private const string TMP_SETTINGS = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        // The UI's own glyphs, by file name in Art/UI/Icons; the name is what a text writes.
        private static readonly string[] EXTRAS = { "hourglass", "padlock", "clock", "compass", "harmony", "Planks", "CutStone", "Iron", "Runestone", "Starmetal", "Heartwood", "Moonglass", "workers", "atk", "dmg", "def", "hp", "army", "power" };

        // The face a glyph is measured against: the Price role's figure.
        private const float POINT_SIZE = 48f;
        private const float GLYPH_HEIGHT = 76f;
        // Where the middle of a Nunito digit sits over the baseline, as a share of the size (cap height / 2).
        private const float DIGIT_MIDDLE = 0.355f;
        // The web's 1 px between a mark and its figure, in rpx.
        private const float GAP = 3f;
        private const int PAD = 2;

        [MenuItem("Kingdom/UI/Rebuild inline icons")]
        public static void Rebuild()
        {
            var icons = Sources();
            var cell = icons.Max(i => (int)Mathf.Max(i.Sprite.rect.width, i.Sprite.rect.height)) + 2 * PAD;
            var columns = Mathf.CeilToInt(Mathf.Sqrt(icons.Count));
            var rows = Mathf.CeilToInt(icons.Count / (float)columns);
            var size = new Vector2Int(Mathf.NextPowerOfTwo(columns * cell), Mathf.NextPowerOfTwo(rows * cell));

            var sheet = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            sheet.SetPixels32(new Color32[size.x * size.y]);
            var rects = new List<RectInt>();
            for (var i = 0; i < icons.Count; i++)
            {
                var pixels = Pixels(icons[i].Sprite);
                var x = i % columns * cell + PAD;
                var y = size.y - (i / columns + 1) * cell + PAD;
                sheet.SetPixels(x, y, pixels.width, pixels.height, pixels.GetPixels());
                rects.Add(new RectInt(x, y, pixels.width, pixels.height));
                Object.DestroyImmediate(pixels);
            }
            sheet.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(SHEET)!);
            File.WriteAllBytes(SHEET, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            AssetDatabase.ImportAsset(SHEET, ImportAssetOptions.ForceUpdate);

            var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(ASSET);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(asset, ASSET);
                // Versioned before it has a material, or TMP takes it for a legacy asset and "upgrades" it.
                var fresh = new SerializedObject(asset);
                fresh.FindProperty("m_Version").stringValue = "1.1.0";
                fresh.ApplyModifiedPropertiesWithoutUndo();
                var material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "Inline Icons Material" };
                AssetDatabase.AddObjectToAsset(material, asset);
                asset.material = material;
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SHEET);
            asset.spriteSheet = texture;
            asset.material.SetTexture(ShaderUtilities.ID_MainTex, texture);
            asset.faceInfo = new FaceInfo { pointSize = (int)POINT_SIZE, scale = 1f, lineHeight = POINT_SIZE, ascentLine = POINT_SIZE, baseline = 0 };

            asset.spriteGlyphTable.Clear();
            asset.spriteCharacterTable.Clear();
            var middle = POINT_SIZE * DIGIT_MIDDLE;
            for (var i = 0; i < icons.Count; i++)
            {
                var r = rects[i];
                var width = GLYPH_HEIGHT * r.width / r.height;
                var glyph = new TMP_SpriteGlyph
                {
                    index = (uint)i,
                    glyphRect = new GlyphRect(r.x, r.y, r.width, r.height),
                    metrics = new GlyphMetrics(width, GLYPH_HEIGHT, 0f, middle + GLYPH_HEIGHT / 2f, width + GAP),
                    scale = 1f,
                };
                asset.spriteGlyphTable.Add(glyph);
                asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xFFFE, glyph) { name = icons[i].Name, scale = 1f });
            }
            asset.UpdateLookupTables();
            EditorUtility.SetDirty(asset);

            var settings = new SerializedObject(AssetDatabase.LoadAssetAtPath<TMP_Settings>(TMP_SETTINGS));
            settings.FindProperty("m_defaultSpriteAsset").objectReferenceValue = asset;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"#UI# {icons.Count} inline icons in {ASSET}.");
        }

        // Every currency's icon by its id, then the UI's glyphs.
        public static List<(string Name, Sprite Sprite)> Sources()
        {
            var currencies = AssetDatabase.FindAssets("t:" + nameof(CurrencyCollection))
                .Select(g => AssetDatabase.LoadAssetAtPath<CurrencyCollection>(AssetDatabase.GUIDToAssetPath(g)))
                .SelectMany(c => c.Entries.OfType<CurrencyAsset>())
                .Where(c => c.Icon != null)
                .Select(c => (c.Id, c.Icon));
            var extras = EXTRAS.Select(n => (n, AssetDatabase.LoadAssetAtPath<Sprite>(ICONS + n + ".png")));
            return currencies.Concat(extras).ToList();
        }

        // A sprite's pixels, read through the GPU so its texture need not be readable.
        private static Texture2D Pixels(Sprite sprite)
        {
            var source = sprite.texture;
            var rect = sprite.textureRect;
            var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, target);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var pixels = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(rect.x, rect.y, rect.width, rect.height), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return pixels;
        }
    }
}
