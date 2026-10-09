using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Sites
{
    // How a kind of landmark is shown: its name and its drawing.
    [Serializable]
    public class LandmarkKindData
    {
        [SerializeField, Required] private string _kind;
        [SerializeField] private string _name;
        [SerializeField, PreviewField(48)] private Sprite _art;

        public string Kind => _kind;
        public string Name => _name;
        public Sprite Art => _art;
    }
}
