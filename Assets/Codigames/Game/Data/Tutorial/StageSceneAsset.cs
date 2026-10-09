using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Tutorial;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Tutorial
{
    // A scene of the stage (Docs/features/24-dialogue.md): list order in its collection is the order scenes are
    // considered in.
    [CreateAssetMenu(fileName = "Scene", menuName = "Kingdom/Data/Scene")]
    public class StageSceneAsset : DefinitionAsset, ISceneDefinition
    {
        [BoxGroup("Start"), SerializeField] private ConditionData _trigger = new(ConditionKind.Always, "", 0);
        [BoxGroup("Start"), SerializeField, Tooltip("It may start over an open sheet.")] private bool _anywhere;
        [BoxGroup("Start"), SerializeField, Tooltip("An introduction; false for a First Morning beat, which runs strictly in order.")]
        private bool _skippable = true;
        [BoxGroup("Start"), SerializeField] private SceneWhere _where;
        [BoxGroup("Start"), SerializeField, Tooltip("Already done: settled unplayed. Unknown for never.")] private ConditionData _doneWhen = new(ConditionKind.Unknown, "", 0);
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)] private List<SceneLineData> _lines = new();

        private IReadOnlyList<ISceneLine> _lineList;

        public Condition Trigger => _trigger.Value;
        public bool Anywhere => _anywhere;
        public bool Skippable => _skippable;
        public SceneWhere Where => _where;
        public Condition DoneWhen => _doneWhen.Value;
        public IReadOnlyList<ISceneLine> Lines => _lineList ??= _lines.Cast<ISceneLine>().ToList();

        protected override void OnValidate()
        {
            base.OnValidate();
            _lineList = null;
        }
    }
}
