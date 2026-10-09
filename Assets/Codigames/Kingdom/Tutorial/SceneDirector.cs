using System.Collections.Generic;
using Codigames.Kingdom.Doors;
using Codigames.Kingdom.Tutorial.State;

namespace Codigames.Kingdom.Tutorial
{
    // Which scene plays next (Docs/features/23-tutorials.md §1):
    // 1. A scene plays where it belongs: the province, the world board or either.
    // 2. One that cannot start here does not hold the others; only the First Morning's beats run strictly in order.
    // 3. What the player has already done is not taught: a scene whose DoneWhen holds when due is settled unplayed.
    // 4. A lesson never asks for what the player cannot pay.
    public class SceneDirector
    {
        // Conditions that record how far the kingdom has got, and so can tell a scene where to resume. The rest (a
        // sheet open, a control on screen, taps since the line began) are moments, not progress.
        private static readonly HashSet<ConditionKind> PROGRESS = new()
        {
            ConditionKind.QuestReached, ConditionKind.QuestComplete, ConditionKind.QuestClaimed, ConditionKind.QuestProgress,
            ConditionKind.TechDone, ConditionKind.TechFilled, ConditionKind.Placed, ConditionKind.Built, ConditionKind.Population,
            ConditionKind.Training, ConditionKind.Heroes, ConditionKind.LairFound, ConditionKind.LairDefeated, ConditionKind.LairCleared,
            ConditionKind.LandmarkClaimed, ConditionKind.LandmarkSeen, ConditionKind.BookOpen, ConditionKind.DoorOpen,
            ConditionKind.Revealed, ConditionKind.TreasureRevealed, ConditionKind.TreasurePicked, ConditionKind.AbandonedRevealed,
            ConditionKind.Repairing, ConditionKind.CanRepair, ConditionKind.WorldVisited, ConditionKind.ReachCleared,
            ConditionKind.Upgraded, ConditionKind.Troops, ConditionKind.RelicHosted, ConditionKind.ExplorerSent,
            ConditionKind.ExplorerRevealed, ConditionKind.HexHeld,
        };

        // Introductions that would only interrupt the First Morning: what stands past the fog, the notices, a full
        // store, an idle crew, a full head of Knowledge. They wait for it to end.
        private static readonly HashSet<ConditionKind> WAITS_FOR_MORNING = new()
        {
            ConditionKind.Sighted, ConditionKind.Ui, ConditionKind.StoreFull, ConditionKind.IdleCrew, ConditionKind.KnowledgeFull,
        };

        private readonly IReadOnlyList<ISceneDefinition> _scenes;
        private readonly TutorialState _state;
        private readonly IConditions _conditions;
        private readonly IScenePurse _purse;
        private readonly IMorning _morning;

        public SceneDirector(IReadOnlyList<ISceneDefinition> scenes, TutorialState state, IConditions conditions, IScenePurse purse,
            IMorning morning)
        {
            _scenes = scenes;
            _state = state;
            _conditions = conditions;
            _purse = purse;
            _morning = morning;
        }

        public static string Key(string sceneId) => "scene:" + sceneId;

        public static bool IsProgress(ConditionKind kind) => PROGRESS.Contains(kind);

        public bool IsPlayed(ISceneDefinition scene) => _state.Seen.Contains(Key(scene.Id));

        public void MarkPlayed(ISceneDefinition scene) => _state.Seen.Add(Key(scene.Id));

        // Is the player on the screen this scene belongs to?
        public static bool InPlace(IStageContext context, ISceneDefinition scene)
            => scene.Where == SceneWhere.Any || (scene.Where == SceneWhere.World) == context.OnWorld;

        // May this scene start here and now? On its own screen, and over a sheet only when it says so.
        public static bool FitsHere(IStageContext context, ISceneDefinition scene)
            => InPlace(context, scene) && (scene.Anywhere || !context.HasOpenSheet);

        public bool AlreadyDone(ISceneDefinition scene) => scene.DoneWhen.IsSet && _conditions.Holds(scene.DoneWhen);

        // The next scene to play. `breathing` is true while the gap after the last scene runs: introductions wait.
        public ScenePick Pick(IStageContext context, bool breathing)
        {
            var settled = new List<ISceneDefinition>();
            if (_state.Veteran) return new ScenePick(null, settled);

            var morning = _morning.FirstMorningOn;
            foreach (var scene in _scenes)
            {
                if (IsPlayed(scene)) continue;
                if (morning && WAITS_FOR_MORNING.Contains(scene.Trigger.Kind)) continue;
                if (!_conditions.Holds(scene.Trigger)) continue;
                if (AlreadyDone(scene))
                {
                    settled.Add(scene);
                    continue;
                }

                if (context.HeldBack) return new ScenePick(null, settled);
                if (scene.Skippable && breathing) return new ScenePick(null, settled);
                if (FitsHere(context, scene) && _purse.CanPayFor(scene)) return new ScenePick(scene, settled);
                // A beat of the First Morning waits its turn, strictly in order.
                if (!scene.Skippable) return new ScenePick(null, settled);
            }

            return new ScenePick(null, settled);
        }
    }
}
