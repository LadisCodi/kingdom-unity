using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Doors
{
    [CreateAssetMenu(fileName = "Unlock", menuName = "Kingdom/Data/Unlock")]
    public class UnlockAsset : DefinitionAsset, IUnlock
    {
        [SerializeField] private UnlockKind _kind;
        [SerializeField, Tooltip("A door (build, research, heroes…) or a book (Sagas, Atlas).")] private string _target;
        [SerializeField] private string _title;
        [SerializeField, TextArea(2, 4)] private string _text;
        [SerializeField, PreviewField(64)] private Sprite _icon;

        public UnlockKind Kind => _kind;
        public string Target => _target;
        public string Title => _title;
        public string Text => _text;
        public Sprite Icon => _icon;
    }
}
