using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Research
{
    // A book of the shelf and its bands.
    [Serializable]
    public class BookData
    {
        [SerializeField] private string _id;
        [SerializeField, Tooltip("The bookmark's emblem.")] private Sprite _emblem;
        [SerializeField, Tooltip("The bookmark's ribbon.")] private Color _tint = Color.white;
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<EraData> _eras = new();

        public string Id => _id;
        public Sprite Emblem => _emblem;
        public Color Tint => _tint;
        public IReadOnlyList<EraData> Eras => _eras;
    }

    // A band of a book: the revealed cells it waits for, and what finishing it pays.
    [Serializable]
    public class EraData
    {
        [SerializeField, MinValue(0), SuffixLabel("cells")] private int _cellsToOpen;
        [SerializeField] private bool _rewarded;
        [SerializeField, MinValue(1), ShowIf(nameof(_rewarded)), SuffixLabel("fragments")] private int _reward;

        public EraData()
        {
        }

        public EraData(int cellsToOpen, int? reward)
        {
            _cellsToOpen = cellsToOpen;
            _rewarded = reward.HasValue;
            _reward = reward ?? 0;
        }

        public int CellsToOpen => _cellsToOpen;
        public int? Reward => _rewarded ? _reward : null;
    }
}
