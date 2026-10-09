using System;
using Codigames.Kingdom.Research;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Research
{
    // One thing a technology opens, as authored.
    [Serializable]
    public class UnlockData
    {
        [SerializeField, HorizontalGroup, HideLabel] private UnlockKind _kind;
        [SerializeField, HorizontalGroup, HideLabel] private string _id;
        [SerializeField, HorizontalGroup(60), HideLabel, ShowIf(nameof(HasLevel))] private int _level;

        public UnlockData()
        {
        }

        public UnlockData(UnlockKind kind, string id, int level)
        {
            _kind = kind;
            _id = id;
            _level = level;
        }

        public TechUnlock ToUnlock() => new(_kind, _id, HasLevel ? _level : 0);

        private bool HasLevel => _kind is UnlockKind.DistrictLevel or UnlockKind.Evolution;
    }
}
