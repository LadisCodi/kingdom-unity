using System.IO;
using Codigames.Modules.Saves;
using UnityEngine;

namespace Codigames.Game.Saves
{
    // Saves as files in the app's persistent folder. A write goes to a temporary file first and then takes the
    // slot's place, so a write cut short leaves the last good save.
    public class FileSaveStorage : ISaveStorage
    {
        private const string FOLDER = "saves";
        private const string EXTENSION = ".json";

        public static string PathOf(string slot) => Path.Combine(Application.persistentDataPath, FOLDER, slot + EXTENSION);

        public string Read(string slot)
        {
            var path = PathOf(slot);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public void Write(string slot, string text)
        {
            var path = PathOf(slot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var temporary = path + ".tmp";
            File.WriteAllText(temporary, text);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }

        public void Keep(string slot, string asSlot)
        {
            var path = PathOf(slot);
            if (File.Exists(path)) File.Copy(path, PathOf(asSlot), true);
        }

        public void Delete(string slot)
        {
            var path = PathOf(slot);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
