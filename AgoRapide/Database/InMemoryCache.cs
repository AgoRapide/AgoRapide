// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.Database {

    /// <summary>
    /// TODO: Now that useCache was removed as parameter to <see cref="BaseDatabase"/>-methods we may 
    /// TODO: add convenience methods here like TryGetEntityById which take a <see cref="BaseDatabase"/> instance as parameter 
    /// TODO: and uses that if result is not found in our cache. 
    /// </summary>
    [Class(
        Description =
            "Supports queries through -" + nameof(QueryId) + "-. " +
            "Utilized by -" + nameof(BaseDatabase) + "- as indicated by -" + nameof(ClassAttribute.CacheUse) + "-.",
        LongDescription =
            "Permanent storage is provided by -" + nameof(BaseDatabase) + "-, maybe in combination with -" + nameof(FileCache) + "-."
        )]
    public class InMemoryCache : BaseCore {

        private InMemoryCache() { }
        public static readonly InMemoryCache instance = new InMemoryCache(); /// Singleton makes for easy inheriting of log-methods from <see cref="BaseCore"/>. Apart from this need for logging the class could have just been made static instead.

        /// <summary>
        /// Cache as relevant for <see cref="CacheUse.Dynamic"/>. 
        /// 
        /// Also used directly by methods such as <see cref="Extensions.AsEntityName(long)"/>
        /// 
        /// TODO: Improve on this situation.
        /// TODO: Make the cache foolproof, that is, ensure that it always returns correct information. 
        /// As of May 2017 the cache is not guaranteed to be 100% correct.
        /// In general the cache should therefore mostly be used for nice-to-have functionality, like showing names instead
        /// of ids in HTML interface without any performance hits. 
        /// The system does however make a "best effort" attempt at keeping the cache up-to-date
        /// and invalidating known no-longer-valid  entries
        /// 
        /// Note subtle point about the entity being stored in the cache, not the root-property (in other words, entity root properties (<see cref="CoreP.RootProperty"/>)
        /// are not found in cache itself)
        /// </summary>
        public static ConcurrentDictionary<long, BaseEntity> EntityCache = new ConcurrentDictionary<long, BaseEntity>();
        /// <summary>
        /// Usually reset is done as a precaution when exceptions occur. 
        /// </summary>
        public static void ResetEntityCache() => EntityCache = new ConcurrentDictionary<long, BaseEntity>();

    }
}
