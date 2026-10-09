using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.Art
{
    // Import settings by folder, so a sound dropped in its folder needs no hand-tuning:
    // - Audio/Sfx and Audio/Voice: short and played often, decompressed when loaded so a play costs nothing.
    // - Audio/Music and Audio/Ambience: long loops, streamed from disk so they take no memory.
    public class AudioImportRules : AssetPostprocessor
    {
        private const string SFX = "Assets/Audio/Sfx/";
        private const string VOICE = "Assets/Audio/Voice/";
        private const string MUSIC = "Assets/Audio/Music/";
        private const string AMBIENCE = "Assets/Audio/Ambience/";

        private void OnPreprocessAudio()
        {
            var shortSound = assetPath.StartsWith(SFX) || assetPath.StartsWith(VOICE);
            var longSound = assetPath.StartsWith(MUSIC) || assetPath.StartsWith(AMBIENCE);
            if (!shortSound && !longSound) return;

            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = shortSound ? 0.7f : 0.6f;
            settings.loadType = shortSound ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.Streaming;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = longSound;
        }
    }
}
