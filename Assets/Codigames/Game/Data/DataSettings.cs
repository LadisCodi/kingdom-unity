using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Codigames.Game.Data
{
    // A group of global numbers of the balance (construction, the fog…), as one asset.
    public abstract class DataSettings : ScriptableObject
    {
        public virtual IEnumerable<string> Problems() => Enumerable.Empty<string>();
    }
}
