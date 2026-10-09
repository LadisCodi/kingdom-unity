using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Tutorial
{
    // A speaker's figure in one mood.
    [Serializable]
    public class ExpressionArt
    {
        [SerializeField, HorizontalGroup, HideLabel] private string _expression;
        [SerializeField, HorizontalGroup, HideLabel, PreviewField(48)] private Sprite _art;

        public ExpressionArt(string expression, Sprite art)
        {
            _expression = expression;
            _art = art;
        }

        public string Expression => _expression;
        public Sprite Art => _art;
    }
}
