using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using System.Reflection;
using AgoRapide.API;

namespace AgoRapide.Database {

    /// <summary>
    /// Note <see cref="BaseInjector"/> assumes <see cref="CacheUse.All"/> for all entities involved and therefore always queries <see cref="InMemoryCache"/> directly.
    /// 
    /// TODO: Move code from <see cref="InMemoryCache.GetMatchingEntities"/> into this class instead.
    /// </summary>
    [Class(
        Description =
            "Responsible for injecting values that are only to be stored dynamically in RAM.\r\n" +
            "That is, values that are not to be stored in database, neither to be synchronized from external source.",
        LongDescription =
            nameof(BaseInjector) + " injects values that can automatically be deduced from standard AgoRapide information\r\n" +
            "(a typical example would be aggregations over foreign keys).\r\n" +
            "Inherited classes inject additional values based on their own C# logic")]
    public class BaseInjector {
    }
}