using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Sites
{
    public static class SiteListExtensions
    {
        // A site by id; null when there is none.
        public static T Find<T>(this IReadOnlyList<T> sites, string id) where T : class, IIdentifiable
        {
            foreach (var site in sites)
            {
                if (site.Id == id) return site;
            }

            return null;
        }
    }
}
