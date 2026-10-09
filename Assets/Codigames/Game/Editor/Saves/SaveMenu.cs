using System.IO;
using Codigames.Game.Saves;
using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.Saves
{
    // The save on this machine, from the editor's menu: shown in the file browser, or deleted for a fresh kingdom.
    public static class SaveMenu
    {
        [MenuItem("Kingdom/Save/Show save file")]
        public static void Show() => EditorUtility.RevealInFinder(FileSaveStorage.PathOf(KingdomSaves.SLOT));

        [MenuItem("Kingdom/Save/Delete save")]
        public static void Delete()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("#Save# Stop playing first: the running game would write it again.");
                return;
            }

            new FileSaveStorage().Delete(KingdomSaves.SLOT);
            Debug.Log("#Save# Deleted; the next Play starts a new kingdom.");
        }
    }
}
