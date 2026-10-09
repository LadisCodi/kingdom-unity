using System;
using Codigames.Kingdom.Research;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Research
{
    // One number a bonus moves, as authored. A percent is in points: 10 is +10%.
    [Serializable]
    public class EffectData
    {
        [SerializeField, HorizontalGroup, HideLabel] private string _stat;
        [SerializeField, HorizontalGroup(80), HideLabel] private EffectOp _op;
        [SerializeField, HorizontalGroup(60), HideLabel, MinValue(0)] private double _value;
        [SerializeField, HorizontalGroup(100), HideLabel] private TargetKind _target;
        [SerializeField, HorizontalGroup, HideLabel, ShowIf(nameof(IsAimed))] private string _targetId;

        public EffectData()
        {
        }

        public EffectData(string stat, EffectOp op, double value, TargetKind target, string targetId)
        {
            _stat = stat;
            _op = op;
            _value = value;
            _target = target;
            _targetId = targetId;
        }

        public TechEffect ToEffect() => new(_stat, _op, _value, _target, IsAimed ? _targetId : null);

        private bool IsAimed => _target != TargetKind.Global;
    }
}
