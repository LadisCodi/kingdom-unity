using System.IO;
using System.Linq;
using Codigames.Game.UI.Kit;
using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.Art
{
    // Fills the UI icon catalog from Art/UI/Icons: each sprite under its file's name, the id a screen asks for.
    public static class UiIconsBuilder
    {
        public const string CATALOG = "Assets/Catalogs/UiIcons.asset";
        private const string ICONS = "Assets/Art/UI/Icons";

        [MenuItem("Kingdom/UI/Rebuild icon catalog")]
        public static void Rebuild()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<UiIcons>(CATALOG);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<UiIcons>();
                AssetDatabase.CreateAsset(catalog, CATALOG);
            }

            var icons = AssetDatabase.FindAssets("t:Sprite", new[] { ICONS })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .Select(p => new UiIcons.Entry(Path.GetFileNameWithoutExtension(p), AssetDatabase.LoadAssetAtPath<Sprite>(p)));
            catalog.Set(icons);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
    }
}
