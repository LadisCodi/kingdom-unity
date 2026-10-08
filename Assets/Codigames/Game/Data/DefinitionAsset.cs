using System.Collections.Generic;
using System.Linq;
using Codigames.Modules.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data
{
    // One entry of a collection of the balance (a building, a currency), as an asset. Its id is what state and
    // saves store; it defaults to the asset's name.
    public abstract class DefinitionAsset : ScriptableObject, IIdentifiable
    {
        [PropertyOrder(-100)]
        [SerializeField, Required] private string _id;

        public string Id => _id;

        // What is wrong with it, by the rules Kingdom states; empty when it is legal.
        public virtual IEnumerable<string> Problems() => Enumerable.Empty<string>();

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(_id)) _id = name;
        }
    }
}
