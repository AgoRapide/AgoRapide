using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {

    [Enum(Description =
        "Describes how aggressively cache should be used when querying a given type of -" + nameof(BaseEntity) + "-. " +
        "(specified by -" + nameof(ClassAttribute.CacheUse) + "-.)")]
    public enum CacheUse {

        [EnumValue(Description = "The most 'safe' (and also default) approach.")]
        None,

        [EnumValue(Description = 
            "Cache as deemed necessary. Items may be added or removed from cache. " +
            "Queries will go through either -" + nameof(InMemoryCache) + "- or -" + nameof(BaseDatabase) + "-. ")]
        Dynamic,

        [EnumValue(Description = 
            "All instances to be cached (usually at application startup). " + 
            "Use when limited number of elements exists or when performance is critical. " + 
            "Obligatory when using -" + nameof(FileCache) + "-. " +
            "Queries will always go through -" + nameof(InMemoryCache) + "-")]
        All
    }
}
