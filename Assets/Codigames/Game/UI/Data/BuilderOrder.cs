using System;
using System.Collections.Generic;

namespace Codigames.Game.UI.Data
{
    // The job the builder sheet was raised for: its verb, its words, its price, and how to start it — or null, when the
    // sheet was opened only to look at the crew.
    public sealed class BuilderOrder
    {
        public string Verb;
        public string What;
        public IReadOnlyList<PriceTerm> Price = Array.Empty<PriceTerm>();
        public Action Start;
    }
}
