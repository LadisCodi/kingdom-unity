using System.IO;
using System.Linq;
using Codigames.Game.Data;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.Data
{
    // The whole balance in one window: every collection with its entries, and every settings group. Problems
    // are shown above the selected item as it is edited.
    public class KingdomDataWindow : OdinMenuEditorWindow
    {
        [MenuItem("Kingdom/Data")]
        private static void Open()
        {
            var window = GetWindow<KingdomDataWindow>("Kingdom Data");
            window.minSize = new Vector2(720, 480);
            window.Show();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(supportsMultiSelect: false);
            tree.Config.DrawSearchToolbar = true;

            foreach (var collection in DataAssets.FindAll<DataCollection>().OrderBy(c => c.Title))
            {
                tree.Add(collection.Title, collection);

                foreach (var entry in collection.Entries)
                {
                    if (entry != null) tree.Add($"{collection.Title}/{entry.Id}", entry);
                }
            }

            foreach (var settings in DataAssets.FindAll<DataSettings>().OrderBy(s => s.name))
            {
                tree.Add($"Settings/{ObjectNames.NicifyVariableName(settings.name)}", settings);
            }

            return tree;
        }

        protected override void OnBeginDrawEditors()
        {
            if (MenuTree == null) return;

            var selected = MenuTree.Selection.SelectedValue;

            SirenixEditorGUI.BeginHorizontalToolbar(MenuTree.Config.SearchToolbarHeight);
            if (selected is DataCollection collection
                && SirenixEditorGUI.ToolbarButton(new GUIContent($"New {ObjectNames.NicifyVariableName(collection.EntryType.Name)}")))
            {
                CreateEntry(collection);
            }

            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Validate all"))) ValidateAll();
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Refresh"))) ForceMenuTreeRebuild();
            SirenixEditorGUI.EndHorizontalToolbar();

            var problems = selected switch
            {
                DefinitionAsset entry => entry.Problems(),
                DataSettings settings => settings.Problems(),
                DataCollection list => DataValidator.Problems(list),
                _ => Enumerable.Empty<string>(),
            };

            foreach (var problem in problems) SirenixEditorGUI.ErrorMessageBox(problem);
        }

        private void CreateEntry(DataCollection collection)
        {
            var folder = $"{DataAssets.ROOT}/{collection.Title}";
            Directory.CreateDirectory(folder);

            var entry = (DefinitionAsset)CreateInstance(collection.EntryType);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/New{collection.EntryType.Name}.asset");
            AssetDatabase.CreateAsset(entry, path);

            collection.Add(entry);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();

            ForceMenuTreeRebuild();
            TrySelectMenuItemWithObject(entry);
        }

        private static void ValidateAll()
        {
            var problems = DataValidator.Problems().ToList();
            if (problems.Count == 0) Debug.Log("#Data# The balance is legal.");
            foreach (var problem in problems) Debug.LogError("#Data# " + problem);
        }
    }
}
