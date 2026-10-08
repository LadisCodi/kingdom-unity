using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Codigames.Game.Editor.Data
{
    // Every asset of a kind in the project, found by type rather than by path.
    public static class DataAssets
    {
        public const string ROOT = "Assets/Data";

        public static IEnumerable<T> FindAll<T>() where T : Object
            => AssetDatabase.FindAssets("t:" + typeof(T).Name)
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null);

        public static T FindFirst<T>() where T : Object => FindAll<T>().FirstOrDefault();
    }
}
