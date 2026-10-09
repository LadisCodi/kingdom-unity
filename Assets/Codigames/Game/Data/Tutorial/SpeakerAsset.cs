using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Tutorial
{
    // A speaker of the stage. Their voice is the sound "voice:<id>_<mood>", else "voice:<id>", in the catalog.
    [CreateAssetMenu(fileName = "Speaker", menuName = "Kingdom/Data/Speaker")]
    public class SpeakerAsset : DefinitionAsset, ISpeaker
    {
        [SerializeField] private string _name;
        [SerializeField] private string _title;
        [SerializeField, PreviewField(96)] private Sprite _portrait;
        [SerializeField] private List<ExpressionArt> _expressions = new();
        [SerializeField, PreviewField(32)] private Sprite _ribbon;

        public string Name => _name;
        public string Title => _title;
        public Sprite Ribbon => _ribbon;

        public Sprite Picture(string expression)
        {
            if (!string.IsNullOrEmpty(expression))
            {
                var mood = _expressions.FirstOrDefault(e => e.Expression == expression);
                if (mood?.Art != null) return mood.Art;
            }

            return _portrait;
        }
    }
}
