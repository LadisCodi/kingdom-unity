using System;
using Codigames.Kingdom.Tutorial;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Tutorial
{
    [Serializable]
    public class SceneLineData : ISceneLine
    {
        [SerializeField, HorizontalGroup("Who")] private string _speaker;
        [SerializeField, HorizontalGroup("Who")] private StageSide _side;
        [SerializeField, HorizontalGroup("Who")] private string _expression = "";
        [SerializeField, TextArea(2, 4)] private string _text;
        [SerializeField, HorizontalGroup("Where")] private string _box = "auto";
        [SerializeField, HorizontalGroup("Where")] private string _point = "";
        [SerializeField, HorizontalGroup("Where")] private LineLock _lock;
        [SerializeField] private ConditionData _until = new(ConditionKind.Tap, "", 0);
        [SerializeField, HorizontalGroup("After")] private bool _exit;
        [SerializeField, HorizontalGroup("After")] private string _stocks = "";

        public SceneLineData(string speaker, StageSide side, string expression, string text, string box, string point, LineLock lineLock,
            ConditionData until, bool exit, string stocks)
        {
            _speaker = speaker;
            _side = side;
            _expression = expression ?? "";
            _text = text ?? "";
            _box = box ?? "auto";
            _point = point ?? "";
            _lock = lineLock;
            _until = until;
            _exit = exit;
            _stocks = stocks ?? "";
        }

        public string Speaker => _speaker;
        public StageSide Side => _side;
        public string Text => _text;
        public string Expression => _expression;
        public string Box => _box;
        public string Point => _point;
        public LineLock Lock => _lock;
        public Condition Until => _until.Value;
        public bool Exit => _exit;
        public string Stocks => _stocks;
    }
}
