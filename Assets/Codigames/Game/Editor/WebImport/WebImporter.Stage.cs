using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data;
using Codigames.Game.Data.Tutorial;
using Codigames.Kingdom.Tutorial;
using UnityEditor;
using UnityEngine;

namespace Codigames.Game.Editor.WebImport
{
    // The stage: its speakers, its scenes in order, and its pacing.
    public static partial class WebImporter
    {
        private const string STAGE_ART = "Assets/Art/UI/Stage/";
        private const string DEFAULT_RIBBON = "blue";

        // Each speaker's ribbon colour, as the web's stage.css gives it; the rest wear the default.
        private static readonly Dictionary<string, string> RIBBONS = new()
        {
            ["advisor"] = "blue", ["warden"] = "green", ["cook"] = "red", ["villager"] = "brown", ["orcChief"] = "crimson",
            ["woodcutter"] = "brown", ["scout"] = "purple",
        };

        private static readonly string[] MOODS = { "happy", "worried", "surprised", "idea", "angry", "sad" };

        private static void ImportStage()
        {
            ImportSpeakers();
            ImportScenes();
            ImportStageSettings();
        }

        private static void ImportSpeakers()
        {
            var assets = new List<DefinitionAsset>();
            foreach (var (id, row) in Read<Dictionary<string, SpeakerData>>("Game/speakers.json"))
            {
                var asset = LoadOrCreate<SpeakerAsset>("Speakers", id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_name").stringValue = row.Name;
                so.FindProperty("_title").stringValue = row.Title;
                so.FindProperty("_portrait").objectReferenceValue = StageSprite(row.Portrait);
                so.FindProperty("_ribbon").objectReferenceValue = StageSprite("ribbon-" + RIBBONS.GetValueOrDefault(id, DEFAULT_RIBBON));

                var moods = MOODS.Select(m => (Mood: m, Art: StageSprite($"{row.Portrait}_{m}"))).Where(m => m.Art != null).ToList();
                var list = so.FindProperty("_expressions");
                list.arraySize = moods.Count;
                for (var i = 0; i < moods.Count; i++)
                {
                    var entry = list.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("_expression").stringValue = moods[i].Mood;
                    entry.FindPropertyRelative("_art").objectReferenceValue = moods[i].Art;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                assets.Add(asset);
            }

            SetEntries(LoadOrCreate<SpeakerCollection>(null, "Speakers"), assets);
        }

        private static void ImportScenes()
        {
            var assets = new List<DefinitionAsset>();
            foreach (var row in Read<List<SceneData>>("Game/scenes.json"))
            {
                var asset = LoadOrCreate<StageSceneAsset>("Scenes", row.Id);
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = row.Id;
                SetCondition(so.FindProperty("_trigger"), row.Trigger, row.TriggerTarget, row.TriggerAmount);
                so.FindProperty("_anywhere").boolValue = row.Anywhere;
                so.FindProperty("_skippable").boolValue = row.Skippable;
                so.FindProperty("_where").enumValueIndex = (int)(row.Where switch
                {
                    "world" => SceneWhere.World,
                    "any" => SceneWhere.Any,
                    _ => SceneWhere.Province,
                });
                SetCondition(so.FindProperty("_doneWhen"), row.DoneWhen, row.DoneTarget, row.DoneAmount);

                var lines = so.FindProperty("_lines");
                lines.arraySize = row.Lines.Count;
                for (var i = 0; i < row.Lines.Count; i++)
                {
                    var doc = row.Lines[i];
                    var line = lines.GetArrayElementAtIndex(i);
                    line.FindPropertyRelative("_speaker").stringValue = doc.Speaker ?? "";
                    line.FindPropertyRelative("_side").enumValueIndex = (int)(doc.Side == "right" ? StageSide.Right : StageSide.Left);
                    line.FindPropertyRelative("_expression").stringValue = doc.Expression ?? "";
                    line.FindPropertyRelative("_text").stringValue = doc.Text ?? "";
                    line.FindPropertyRelative("_box").stringValue = doc.Box ?? "auto";
                    line.FindPropertyRelative("_point").stringValue = doc.Point ?? "";
                    line.FindPropertyRelative("_lock").enumValueIndex = (int)(doc.Lock switch
                    {
                        "map" => LineLock.Map,
                        "target" => LineLock.Target,
                        "all" => LineLock.All,
                        _ => LineLock.None,
                    });
                    SetCondition(line.FindPropertyRelative("_until"), doc.Until, doc.UntilTarget, doc.UntilAmount);
                    line.FindPropertyRelative("_exit").boolValue = doc.Exit;
                    line.FindPropertyRelative("_stocks").stringValue = doc.Stocks ?? "";
                    if (!string.IsNullOrEmpty(doc.Gives) || !string.IsNullOrEmpty(doc.Restores))
                        Debug.LogWarning($"#Data# Scene {row.Id} line {i} gives or restores something the stage cannot hand over yet.");
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                assets.Add(asset);
            }

            SetEntries(LoadOrCreate<SceneCollection>(null, "Scenes"), assets);
        }

        private static void ImportStageSettings()
        {
            var help = Read<TutorialData>("Game/tutorial.json").Help;
            var so = new SerializedObject(LoadOrCreate<StageSettingsAsset>("Settings", "Stage"));
            so.FindProperty("_idleWiggleSeconds").doubleValue = help.IdleWiggleSeconds;
            so.FindProperty("_idleAdvisorSeconds").doubleValue = help.IdleAdvisorSeconds;
            so.FindProperty("_advisorRestSeconds").doubleValue = help.AdvisorRestSeconds;
            so.FindProperty("_advisorShowSeconds").doubleValue = help.AdvisorShowSeconds;
            so.FindProperty("_pointerSeconds").doubleValue = help.PointerSeconds;
            so.FindProperty("_untilQuest").stringValue = help.UntilQuest;
            so.FindProperty("_lockFailsafeSeconds").doubleValue = help.LockFailsafeSeconds;
            so.FindProperty("_typeCharsPerSecond").doubleValue = help.TypeCharsPerSecond;
            so.FindProperty("_sceneGapSeconds").doubleValue = help.SceneGapSeconds;
            so.FindProperty("_inputGraceSeconds").doubleValue = help.InputGraceSeconds;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // A condition by its web name; an empty or unknown one is Unknown (never holds), and a name nothing here
        // knows is reported.
        private static void SetCondition(SerializedProperty condition, string kind, string target, double amount)
        {
            var parsed = Condition.Parse(kind);
            if (parsed == ConditionKind.Unknown && !string.IsNullOrEmpty(kind)) Debug.LogWarning($"#Data# Unknown scene condition \"{kind}\".");
            condition.FindPropertyRelative("_kind").enumValueIndex = (int)parsed;
            condition.FindPropertyRelative("_target").stringValue = target ?? "";
            condition.FindPropertyRelative("_amount").doubleValue = amount;
        }

        private static Sprite StageSprite(string stem) => AssetDatabase.LoadAssetAtPath<Sprite>($"{STAGE_ART}{stem}.png");
    }
}
