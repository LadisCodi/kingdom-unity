using System;
using Codigames.Kingdom.Tutorial;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Tutorial
{
    // A condition as authored: a kind, what it names, how many.
    [Serializable]
    public class ConditionData
    {
        [SerializeField, HorizontalGroup, HideLabel] private ConditionKind _kind;
        [SerializeField, HorizontalGroup, HideLabel] private string _target = "";
        [SerializeField, HorizontalGroup(60), HideLabel] private double _amount;

        public ConditionData(ConditionKind kind, string target, double amount)
        {
            _kind = kind;
            _target = target ?? "";
            _amount = amount;
        }

        public Condition Value => new(_kind, _target, _amount);
    }
}
