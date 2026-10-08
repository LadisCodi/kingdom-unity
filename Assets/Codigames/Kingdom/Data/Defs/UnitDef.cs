using System.Collections.Generic;

namespace Codigames.Kingdom.Data
{
    // A unit type at rank I, with its tags and the technology it waits on.
    public sealed class UnitDef
    {
        public string Id { get; set; }
        public IReadOnlyList<string> Tags { get; set; }
        public UnitData Data { get; set; }
        public string RequiredTech { get; set; }
    }
}
