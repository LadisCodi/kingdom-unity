using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codigames.Game.Data.Harvest;
using Codigames.Game.Map;
using Codigames.Game.Startup;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Editor.WebImport
{
    // One-off: paints the web prototype's province (Tools/WebData/region-map.json) into the Province prefab's
    // tilemaps. After that the prefab is the map, edited in Unity with the tile palette.
    public static class MapImporter
    {
        private const string PREFAB = "Assets/Prefabs/Map/Province.prefab";
        private const string TERRAIN_TILES = "Assets/Art/Terrain/Tiles";
        private const string FEATURE_TILES = "Assets/Art/Features/Tiles";
        private const string GAME_SCENE = "Assets/Scenes/Game.unity";
        private const string RENDERER_2D = "Assets/Settings/Renderer2D.asset";

        // Each feature's art, by id: the stem of its sprites (variants are <stem>_2, <stem>_3…).
        private static readonly Dictionary<string, string> FEATURE_ART = new()
        {
            ["Trees"] = "forest", ["Mountain"] = "mountain", ["MountainIron"] = "mountain_iron",
            ["MountainGold"] = "mountain_gold", ["BerryBush"] = "berry_bush", ["WildAnimals"] = "wild_animals",
            ["FishShoal"] = "fish_shoal", ["Crops"] = "farmlands",
        };

        private sealed class RegionMap
        {
            public MapLayerDoc terrain;
            public MapLayerDoc features;
        }

        private sealed class MapLayerDoc
        {
            public List<Cell> cells;
        }

        private sealed class Cell
        {
            public int x;
            public int y;
            public string id;
        }

        [MenuItem("Kingdom/Import web prototype map")]
        public static void Import()
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "WebData", "region-map.json"));
            var map = JsonConvert.DeserializeObject<RegionMap>(File.ReadAllText(path));

            var terrainTiles = map.terrain.cells.Select(c => c.id).Distinct()
                .ToDictionary(id => id, id => Tile(TERRAIN_TILES, id, "Assets/Art/Terrain", "terrain_" + id.ToLowerInvariant()));
            var featureTiles = map.features.cells.Select(c => c.id).Distinct().Where(FEATURE_ART.ContainsKey)
                .ToDictionary(id => id, id => Tile(FEATURE_TILES, id, "Assets/Art/Features", FEATURE_ART[id]));

            var root = PrefabRoot();
            var province = root.GetComponent<ProvinceMap>();
            Paint(province.Terrain, map.terrain.cells, terrainTiles);
            Paint(province.Features, map.features.cells, featureTiles);
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
            Object.DestroyImmediate(root);

            ImportFeatureLooks();
            SortByScreenHeight();
            PlaceInGameScene();
            AssetDatabase.SaveAssets();
            Debug.Log($"#Map# Imported {map.terrain.cells.Count} terrain cells and {map.features.cells.Count} features.");
        }

        // Each feature's other drawings, onto its data: emptied (<stem>_exhausted, and _exhausted_2x2 for a block), and
        // coming up (<stem>_growing1, _growing2…). A planted feature with no tile of its own gets one.
        [MenuItem("Kingdom/Import feature looks")]
        public static void ImportFeatureLooks()
        {
            foreach (var (id, stem) in FEATURE_ART)
            {
                var asset = AssetDatabase.LoadAssetAtPath<FeatureAsset>($"Assets/Data/Features/{id}.asset");
                if (asset == null) continue;

                var so = new SerializedObject(asset);
                if (so.FindProperty("_tile").objectReferenceValue == null)
                    so.FindProperty("_tile").objectReferenceValue = Tile(FEATURE_TILES, id, "Assets/Art/Features", stem);

                so.FindProperty("_exhaustedTile").objectReferenceValue = Look(id, $"{stem}_exhausted", "exhausted");
                var blocks = so.FindProperty("_exhaustedBlockTiles");
                blocks.arraySize = 0;
                for (var size = 2; size <= 3; size++)
                {
                    var block = Look(id, $"{stem}_exhausted_{size}x{size}", $"exhausted_{size}x{size}");
                    if (block == null) break;
                    blocks.arraySize = size - 1;
                    blocks.GetArrayElementAtIndex(size - 2).objectReferenceValue = block;
                }

                var growing = so.FindProperty("_growingTiles");
                growing.arraySize = 0;
                for (var stage = 1; ; stage++)
                {
                    var tile = Look(id, $"{stem}_growing{stage}", $"growing{stage}");
                    if (tile == null) break;
                    growing.arraySize = stage;
                    growing.GetArrayElementAtIndex(stage - 1).objectReferenceValue = tile;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
        }

        // One drawing of a feature as its own tile (<Id>_<look>), or null when the art does not exist.
        private static VariantTile Look(string id, string art, string look)
            => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Features/{art}.png") == null
                ? null
                : Tile(FEATURE_TILES, $"{id}_{look}", id, "Assets/Art/Features", art);

        // A feature's block drawing (<stem>_2x2.png), as a tile with the feature's id; null when it has none.
        internal static VariantTile BlockTile(string featureId, int size)
        {
            if (!FEATURE_ART.TryGetValue(featureId, out var stem)) return null;
            var art = $"{stem}_{size}x{size}";
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Features/{art}.png") == null
                ? null
                : Tile(FEATURE_TILES, $"{featureId}_{size}x{size}", featureId, "Assets/Art/Features", art);
        }

        private static VariantTile Tile(string folder, string id, string artFolder, string stem) => Tile(folder, id, id, artFolder, stem);

        private static VariantTile Tile(string folder, string file, string id, string artFolder, string stem)
        {
            Directory.CreateDirectory(folder);
            var path = $"{folder}/{file}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<VariantTile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<VariantTile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            var variants = new List<Sprite>();
            var first = AssetDatabase.LoadAssetAtPath<Sprite>($"{artFolder}/{stem}.png");
            if (first != null) variants.Add(first);
            for (var n = 2; n <= 4; n++)
            {
                var variant = AssetDatabase.LoadAssetAtPath<Sprite>($"{artFolder}/{stem}_{n}.png");
                if (variant != null) variants.Add(variant);
            }

            var so = new SerializedObject(tile);
            so.FindProperty("_id").stringValue = id;
            var list = so.FindProperty("_variants");
            list.arraySize = variants.Count;
            for (var i = 0; i < variants.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return tile;
        }

        // The prefab's contents, built on first import: a grid with a terrain layer and a features layer.
        private static GameObject PrefabRoot()
        {
            if (File.Exists(PREFAB)) return PrefabUtility.LoadPrefabContents(PREFAB);

            Directory.CreateDirectory(Path.GetDirectoryName(PREFAB));
            var root = new GameObject("Province");
            var grid = root.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);

            var terrain = AddLayer(root, "Terrain", -100, TilemapRenderer.Mode.Chunk, new Vector3(0.5f, 0.5f, 0f));
            var features = AddLayer(root, "Features", 0, TilemapRenderer.Mode.Individual, new Vector3(0f, 0f, 0f));

            var province = root.AddComponent<ProvinceMap>();
            var so = new SerializedObject(province);
            so.FindProperty("_grid").objectReferenceValue = grid;
            so.FindProperty("_terrain").objectReferenceValue = terrain;
            so.FindProperty("_features").objectReferenceValue = features;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
            Object.DestroyImmediate(root);
            return PrefabUtility.LoadPrefabContents(PREFAB);
        }

        private static Tilemap AddLayer(GameObject root, string name, int order, TilemapRenderer.Mode mode, Vector3 anchor)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(root.transform, false);
            var tilemap = layer.AddComponent<Tilemap>();
            tilemap.tileAnchor = anchor;
            var renderer = layer.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = mode;
            return tilemap;
        }

        private static void Paint(Tilemap layer, List<Cell> cells, Dictionary<string, VariantTile> tiles)
        {
            layer.ClearAllTiles();
            foreach (var cell in cells)
            {
                if (tiles.TryGetValue(cell.id, out var tile))
                    layer.SetTile(ProvinceCoordinates.ToTilemap(new ModuleVector2Int(cell.x, cell.y)), tile);
            }

            layer.CompressBounds();
        }

        // Isometric depth: what stands lower on screen is in front.
        private static void SortByScreenHeight()
        {
            var renderer = AssetDatabase.LoadMainAssetAtPath(RENDERER_2D);
            if (renderer == null) return;

            var so = new SerializedObject(renderer);
            so.FindProperty("m_TransparencySortMode").enumValueIndex = (int)TransparencySortMode.CustomAxis;
            so.FindProperty("m_TransparencySortAxis").vector3Value = new Vector3(0f, 1f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PlaceInGameScene()
        {
            var scene = EditorSceneManager.OpenScene(GAME_SCENE);
            var province = Object.FindAnyObjectByType<ProvinceMap>();
            if (province == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
                province = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<ProvinceMap>();
            }

            var scope = Object.FindAnyObjectByType<GameLifetimeScope>();
            var so = new SerializedObject(scope);
            var field = so.FindProperty("_province");
            if (field != null)
            {
                field.objectReferenceValue = province;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(scene);
        }
    }
}
