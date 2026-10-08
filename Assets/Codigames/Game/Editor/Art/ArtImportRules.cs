using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.Art
{
    // Import settings by folder, so a sprite dropped in its folder needs no hand-tuning:
    // - Art/Terrain: one cell's diamond, 256 px wide = one world unit, pivot at the centre.
    // - Art/Features: standing on their cell, pivot at the bottom centre, as wide as their footprint
    //   (a "_2x2" / "_3x3" suffix says how many cells across).
    // - Art/Buildings: pivot at the bottom centre; the view scales each to its footprint.
    // - Art/UI: sprites at 100 px a unit; a piece drawn three-sliced keeps its ends (its border below).
    public class ArtImportRules : AssetPostprocessor
    {
        // The width of each three-sliced piece's ends, in pixels, as the web drew them.
        private static readonly System.Collections.Generic.Dictionary<string, int> SLICED_ENDS = new()
        {
            ["bar-base"] = 54, ["bar-fill-blue"] = 54, ["bar-fill-green"] = 54, ["bar-border"] = 64,
        };

        private const string TERRAIN = "Assets/Art/Terrain/";
        private const string FEATURES = "Assets/Art/Features/";
        private const string BUILDINGS = "Assets/Art/Buildings/";
        private const string UI = "Assets/Art/UI/";

        private void OnPreprocessTexture()
        {
            var isTerrain = assetPath.StartsWith(TERRAIN);
            var isFeature = assetPath.StartsWith(FEATURES);
            var isBuilding = assetPath.StartsWith(BUILDINGS);
            var isUi = assetPath.StartsWith(UI);
            if (!isTerrain && !isFeature && !isBuilding && !isUi) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;

            if (isUi)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.spritePixelsPerUnit = 100;
                var stem = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (SLICED_ENDS.TryGetValue(stem, out var end)) settings.spriteBorder = new Vector4(end, 0, end, 0);
            }
            else if (isTerrain)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.spritePixelsPerUnit = 256;
            }
            else
            {
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                importer.GetSourceTextureWidthAndHeight(out var width, out _);
                importer.spritePixelsPerUnit = isFeature ? width / (float)FootprintCells(assetPath) : 100;
            }

            importer.SetTextureSettings(settings);
        }

        private static int FootprintCells(string path)
        {
            if (path.Contains("_3x3")) return 3;
            if (path.Contains("_2x2")) return 2;
            return 1;
        }
    }
}
