using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.Art
{
    // Import settings by folder, so a sprite dropped in its folder needs no hand-tuning:
    // - Art/Terrain: one cell's diamond, 256 px wide = one world unit, pivot at the centre.
    // - Art/Features: standing on their cell, pivot at the bottom centre, as wide as their footprint
    //   (a "_2x2" / "_3x3" suffix says how many cells across).
    // - Art/Buildings: pivot at the bottom centre; the view scales each to its footprint.
    // - Art/Fog: the clouds and the floor's diamond, centred on their cell, one cell wide (a cloud's tile scales it).
    // - Art/World: things drawn over the map (the collect bubble): pivot at the bottom centre, 100 px a unit.
    // - Art/UI: sprites at 100 px a unit; a sliced piece keeps its border (below). plate-fill and plate-rim are
    //   a white rounded plate and its rim, generated, tinted wherever a card or a chip needs one.
    public class ArtImportRules : AssetPostprocessor
    {
        // Every label button's slab: one 557 × 188 canvas, its rounded ends and lip in the slices.
        private static readonly Vector4 SLAB = new(90, 90, 90, 70);

        // The border each sliced piece keeps, in source pixels (left, bottom, right, top), as the web slices it.
        private static readonly System.Collections.Generic.Dictionary<string, Vector4> SLICED = new()
        {
            ["bar-base"] = Ends(54), ["bar-fill-blue"] = Ends(54), ["bar-fill-green"] = Ends(54), ["bar-border"] = Ends(64),
            ["hud-slot"] = Ends(50),
            ["nav-tab"] = new Vector4(30, 50, 30, 30), ["nav-tab-down"] = new Vector4(30, 50, 30, 30),
            ["window-frame"] = new Vector4(64, 64, 64, 64), ["window-header"] = Ends(60),
            ["plate-fill"] = new Vector4(26, 26, 26, 26), ["plate-rim"] = new Vector4(26, 26, 26, 26),
            ["btn-paint-green"] = SLAB, ["btn-paint-green-down"] = SLAB, ["btn-paint-green-off"] = SLAB,
        };

        private static Vector4 Ends(int width) => new(width, 0, width, 0);

        private const string TERRAIN = "Assets/Art/Terrain/";
        private const string FEATURES = "Assets/Art/Features/";
        private const string BUILDINGS = "Assets/Art/Buildings/";
        private const string UI = "Assets/Art/UI/";
        private const string WORLD = "Assets/Art/World/";
        private const string FOG = "Assets/Art/Fog/";

        private void OnPreprocessTexture()
        {
            var isTerrain = assetPath.StartsWith(TERRAIN);
            var isFeature = assetPath.StartsWith(FEATURES);
            var isBuilding = assetPath.StartsWith(BUILDINGS);
            var isUi = assetPath.StartsWith(UI);
            var isWorld = assetPath.StartsWith(WORLD);
            var isFog = assetPath.StartsWith(FOG);
            if (!isTerrain && !isFeature && !isBuilding && !isUi && !isWorld && !isFog) return;

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
                settings.spritePixelsPerUnit = 100;
                var stem = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (SLICED.TryGetValue(stem, out var border)) settings.spriteBorder = border;
            }
            else if (isFog)
            {
                // Centred on its cell, one cell wide (a cloud's tile scales it): a cloud kept round its own cell
                // never climbs over the cleared ground behind it.
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.GetSourceTextureWidthAndHeight(out var width, out _);
                settings.spritePixelsPerUnit = width;
            }
            else if (isTerrain)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePixelsPerUnit = 256;
            }
            else
            {
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                importer.GetSourceTextureWidthAndHeight(out var width, out _);
                settings.spritePixelsPerUnit = isFeature ? width / (float)FootprintCells(assetPath) : 100;
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
