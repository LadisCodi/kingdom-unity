using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.Art
{
    // Import settings by folder, so a sprite dropped in its folder needs no hand-tuning:
    // - Art/Terrain: one cell's diamond, 256 px wide = one world unit, pivot at the centre.
    // - Art/Features: standing on their cell's bottom corner (pivot at the bottom centre), their canvas two
    //   plots across their footprint (a "_2x2" / "_3x3" suffix says how many cells), one for a crop plot.
    // - Art/Buildings: pivot at the bottom centre; the view scales each to its footprint.
    // - Art/Characters: animation frames planted by the feet (characters.json), a person 0.38 of a plot tall.
    // - Art/Fog: the clouds and the floor's diamond, centred on their cell, one cell wide (a cloud's tile scales it).
    // - Art/World: things drawn over the map (the collect bubble): pivot at the bottom centre, 100 px a unit.
    // - Art/UI: sprites at 100 px a unit; a sliced piece keeps its border (below). plate-fill and plate-rim are
    //   a white rounded plate and its rim, generated, tinted wherever a card or a chip needs one; disc-fill and
    //   disc-rim the same in the round (a portrait), sliced so the rim keeps its width at any size.
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
            ["disc-fill"] = new Vector4(63, 63, 63, 63), ["disc-rim"] = new Vector4(63, 63, 63, 63),
            ["plate-parchment"] = new Vector4(64, 64, 64, 64),
            ["rb-page"] = new Vector4(56, 56, 56, 56), ["hud-know-tab"] = new Vector4(64, 70, 64, 20), ["know-frame"] = Ends(60),
            ["dialogue-frame"] = new Vector4(100, 100, 100, 100),
            ["ribbon-blue"] = Ends(100), ["ribbon-brown"] = Ends(100), ["ribbon-crimson"] = Ends(100), ["ribbon-green"] = Ends(100),
            ["ribbon-purple"] = Ends(100), ["ribbon-red"] = Ends(100), ["stage-halo"] = new Vector4(64, 64, 64, 64),
            ["scroll-parchment"] = new Vector4(100, 100, 100, 110), ["bar-fill-gold"] = Ends(54),
            // A level's enamel plaque (the upgrade sheet), 60 px caps and 70 px ends.
            ["plaque-blue"] = new Vector4(70, 60, 70, 60), ["plaque-green"] = new Vector4(70, 60, 70, 60),
            // A countdown's nailed plaque (a standing notice, an offer): its nailed ends.
            ["offer-plaque"] = Ends(60),
        };

        private static Vector4 Ends(int width) => new(width, 0, width, 0);

        // A label button's slab, any material or state: btn-<material>[-down|-off]. The round close and move are not slabs.
        private static bool IsSlab(string stem) => stem.StartsWith("btn-") && !stem.StartsWith("btn-close") && !stem.StartsWith("btn-move");

        private const string TERRAIN = "Assets/Art/Terrain/";
        private const string FEATURES = "Assets/Art/Features/";
        private const string BUILDINGS = "Assets/Art/Buildings/";
        private const string UI = "Assets/Art/UI/";
        private const string WORLD = "Assets/Art/World/";
        private const string ONE_PLOT_FEATURE = "farmlands";
        private const string CHARACTERS = "Assets/Art/Characters/";
        private const string CHARACTER_FEET = CHARACTERS + "characters.json";

        // How tall a person stands, as a fraction of a plot's width: a little under a cottage's door, as the web
        // draws them (characters.ts UNIT_PLOTS).
        private const float PERSON_PLOTS = 0.38f;
        private const string FOG = "Assets/Art/Fog/";

        private void OnPreprocessTexture()
        {
            var isTerrain = assetPath.StartsWith(TERRAIN);
            var isFeature = assetPath.StartsWith(FEATURES);
            var isBuilding = assetPath.StartsWith(BUILDINGS);
            var isUi = assetPath.StartsWith(UI);
            var isWorld = assetPath.StartsWith(WORLD);
            var isFog = assetPath.StartsWith(FOG);
            var isCharacter = assetPath.StartsWith(CHARACTERS);
            if (!isTerrain && !isFeature && !isBuilding && !isUi && !isWorld && !isFog && !isCharacter) return;

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
                else if (IsSlab(stem)) settings.spriteBorder = SLAB;
            }
            else if (isCharacter)
            {
                // Planted by the feet (characters.json, from the web's atlas index), a person 0.38 of a plot tall.
                var stem = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                importer.GetSourceTextureWidthAndHeight(out var width, out _);
                var feet = Feet(stem);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(feet.X / width, 0f);
                settings.spritePixelsPerUnit = feet.Height / PERSON_PLOTS;
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
                settings.spritePixelsPerUnit = isFeature ? width / (float)(FeatureCanvasPlots(assetPath) * FootprintCells(assetPath)) : 100;
            }

            importer.SetTextureSettings(settings);
        }

        // A feature's canvas is two plots across, the thing somewhere inside it (a stand of trees spreads past its
        // own ground, a boar covers a fraction of it), as the web authors them. A crop plot's art is a building's:
        // one plot across.
        private static int FeatureCanvasPlots(string path)
            => System.IO.Path.GetFileNameWithoutExtension(path).StartsWith(ONE_PLOT_FEATURE) ? 1 : 2;

        private static (float X, float Height) Feet(string stem)
        {
            if (!System.IO.File.Exists(CHARACTER_FEET)) return (0, 128);

            var all = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(CHARACTER_FEET));
            var frame = all[stem];
            return frame == null ? (0, 128) : (frame.Value<float>("feet"), frame.Value<float>("height"));
        }

        private static int FootprintCells(string path)
        {
            if (path.Contains("_3x3")) return 3;
            if (path.Contains("_2x2")) return 2;
            return 1;
        }
    }
}
