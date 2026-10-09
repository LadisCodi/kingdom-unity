using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Tutorial
{
    [CreateAssetMenu(fileName = "Stage", menuName = "Kingdom/Data/Stage Settings")]
    public class StageSettingsAsset : DataSettings, IStageSettings
    {
        [BoxGroup("Idle help"), SerializeField, MinValue(1), SuffixLabel("s")] private double _idleWiggleSeconds = 30;
        [BoxGroup("Idle help"), SerializeField, MinValue(1), SuffixLabel("s")] private double _idleAdvisorSeconds = 60;
        [BoxGroup("Idle help"), SerializeField, MinValue(1), SuffixLabel("s")] private double _advisorRestSeconds = 180;
        [BoxGroup("Idle help"), SerializeField, MinValue(1), SuffixLabel("s")] private double _advisorShowSeconds = 10;
        [BoxGroup("Idle help"), SerializeField, MinValue(1), SuffixLabel("s")] private double _pointerSeconds = 20;
        [BoxGroup("Idle help"), SerializeField] private string _untilQuest = "Attuned";
        [BoxGroup("Lines"), SerializeField, MinValue(1), SuffixLabel("s")] private double _lockFailsafeSeconds = 5;
        [BoxGroup("Lines"), SerializeField, MinValue(1), SuffixLabel("chars / s")] private double _typeCharsPerSecond = 40;
        [BoxGroup("Lines"), SerializeField, MinValue(0), SuffixLabel("s")] private double _sceneGapSeconds = 6;
        [BoxGroup("Lines"), SerializeField, MinValue(0), SuffixLabel("s")] private double _inputGraceSeconds = 0.5;

        public double IdleWiggleSeconds => _idleWiggleSeconds;
        public double IdleAdvisorSeconds => _idleAdvisorSeconds;
        public double AdvisorRestSeconds => _advisorRestSeconds;
        public double AdvisorShowSeconds => _advisorShowSeconds;
        public double PointerSeconds => _pointerSeconds;
        public string UntilQuest => _untilQuest;
        public double LockFailsafeSeconds => _lockFailsafeSeconds;
        public double TypeCharsPerSecond => _typeCharsPerSecond;
        public double SceneGapSeconds => _sceneGapSeconds;
        public double InputGraceSeconds => _inputGraceSeconds;
    }
}
