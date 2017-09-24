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
    /// TODO: Now that useCache was removed as parameter to <see cref="BaseDatabase"/>-methods. 
    /// TODO: We could add convenience methods here like TryGetEntityById which take a <see cref="BaseDatabase"/> instance as parameter 
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
        /// Also used directly by methods such as <see cref="Extensions.AsEntityName(long)"/>
        /// 
        /// ===============================
        /// TODO: Improve on this situation.
        /// TODO: Make the cache foolproof, that is, ensure that it always returns correct information. 
        /// ===============================
        /// As of May 2017 the cache is not guaranteed to be 100% correct.
        /// In general the cache should therefore mostly be used for nice-to-have functionality, like showing names instead
        /// of ids in HTML interface without any performance hits. 
        /// The system does however make a "best effort" attempt at keeping the cache up-to-date
        /// and invalidating known no-longer-valid  entries
        /// ===============================
        /// 
        /// Note subtle point about the entity being stored in the cache, not the root-property 
        /// (in other words, entity root properties (<see cref="CoreP.RootProperty"/>) are not found in cache itself (but as <see cref="BaseEntity.RootProperty"/>))
        /// </summary>
        [ClassMember(Description = "Cache as relevant for -" + nameof(CacheUse.Dynamic) + "- and -" + nameof(CacheUse.All) + "-.")]
        public static ConcurrentDictionary<long, BaseEntity> EntityCache = new ConcurrentDictionary<long, BaseEntity>();

        /// <summary>
        /// Usually reset is done as a precaution when exceptions occur. 
        /// </summary>
        public static void ResetEntityCache() => EntityCache = new ConcurrentDictionary<long, BaseEntity>();

        private static List<Type> _synchronizerTypes = new List<Type>();
        /// <summary>
        /// Call this method for every <see cref="BaseSynchronizer"/>-type that your application uses. 
        /// Not thread-safe. Only to be used by single thread at application initialization. 
        /// </summary>
        /// <param name="synchronizer"></param>
        public static void AddSynchronizerType(Type synchronizer) {
            InvalidTypeException.AssertAssignable(synchronizer, typeof(BaseSynchronizer));
            _synchronizerTypes.Add(synchronizer);
        }
        private static Dictionary<Type, BaseSynchronizer> _synchronizersByType;
        /// <summary>
        /// Returns (cached) information about which <see cref="BaseSynchronizer"/> to use for which type.
        /// 
        /// TODO: Note some unresolved issues here. 
        /// TODO: Should for instance a given type be restricted to only one <see cref="BaseSynchronizer"/> reading that type?
        /// TODO: (see note in exceptioin message below)
        /// TODO: Note that improvements are assumed to be possible to solve internally within <see cref="InMemoryCache"/>, that is without changing the external interface. 
        /// </summary>
        /// <param name="db">
        /// Only used at first call. Ignored for subsequent calls.
        /// </param>
        /// <returns></returns>
        private static Dictionary<Type, BaseSynchronizer> GetSyncronizers(BaseDatabase db) => _synchronizersByType ?? (_synchronizersByType = new Func<Dictionary<Type, BaseSynchronizer>>(() => {
            var retval = new Dictionary<Type, BaseSynchronizer>();
            if (_synchronizerTypes.Count == 0) throw new InMemoryCacheException("No calls made to -" + nameof(AddSynchronizerType) + "-. Should have been done at application startup.");
            _synchronizerTypes.ForEach(t => {
                db.GetAllEntities(t).ForEach(s => {
                    s.PV(SynchronizerP.SynchronizerExternalType.A(), new List<Type>()).ForEach(et => {
                        if (retval.TryGetValue(et, out var duplicate)) { /// Note how this problem is only caught at initial call to <see cref="GetSyncronizers"/>
                            throw new InMemoryCacheException("More than one " + nameof(BaseSynchronizer) + " found for type " + et + ", both\r\n" +
                                "1) " + s + "\r\n" +
                                "and\r\n" +
                                "2) " + duplicate + "\r\n" +
                                "Possible resolution:\r\n" +
                                API.APICommandCreator.JSONInstance.CreateAPIUrl(CoreAPIMethod.PropertyOperation, typeof(Property), new QueryIdInteger(s.Id), PropertyOperation.SetInvalid) + "\r\n" +
                                "or\r\n" +
                                API.APICommandCreator.JSONInstance.CreateAPIUrl(CoreAPIMethod.PropertyOperation, typeof(Property), new QueryIdInteger(duplicate.Id), PropertyOperation.SetInvalid) +
                                (s.GetType().Equals(duplicate.GetType()) ? "" :
                                    ("\r\n(The types are not equal. Possible TODO in AgoRapide is to actually allow multiple " + nameof(BaseSynchronizer) + " as long as they are of different types)"))
                            );
                        }
                        retval.AddValue(et, (BaseSynchronizer)s);
                    });
                });
            });
            return retval;
        })());

        public static ConcurrentDictionary<
            Type,
            /// TRUE means <see cref="BaseSynchronizer"/> exists, FALSE means no <see cref="BaseSynchronizer"/> exists.
            /// TODO: Note how this is (as of Sep 2017) decided only once in application lifetime
            /// TODO: Fix so that can add <see cref="BaseSynchronizer"/> later within application lifetime.
            bool
        > _synchronizedTypes = new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// Used inside GetOrAdd for <see cref="_synchronizedTypes"/>
        /// </summary>
        public static ConcurrentDictionary<Type, bool> _synchronizedTypesInternal = new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// Returns all entities of <paramref name="type"/> matching <paramref name="id"/>
        /// 
        /// Use this for <see cref="CacheUse.All"/>
        /// 
        /// Calls <see cref="FileCache.TryEnrichFromDisk"/> and also <see cref="BaseSynchronizer.Synchronize"/> as needed.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<BaseEntity> GetMatchingEntities(Type type, QueryId id, BaseDatabase db) {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (db == null) throw new ArgumentNullException(nameof(db));
            _synchronizedTypes.GetOrAdd(type, t => { // Note how actual return value, TRUE / FALSE, is ignored.
                if (!GetSyncronizers(db).TryGetValue(t, out var s)) {
                    // This is either because 
                    // 1) Synchronizing is not relevant for this type, or
                    // 2) No synchronizers has been created in the database yet.
                    // TODO: Note how this is (as of Sep 2017) decided permanently for the rest of the application lifetime. 
                    // TODO: But for case 2) above this is of course not good enough.
                    return false;
                }
                lock (_synchronizedTypesInternal) { // Ensure that this "universe" completes first
                    if (_synchronizedTypesInternal.TryGetValue(t, out _)) return true; // Already done by other call (possible on other thread) within same universe

                    var types = s.PV<List<Type>>(SynchronizerP.SynchronizerExternalType.A());
                    var synchronizerWasCalled = false;
                    var allEntities = new Dictionary<Type, List<BaseEntity>>();
                    foreach (var st in types) {
                        if (FileCache.Instance.TryEnrichFromDisk(s, st, db, out var entities, out _)) { // Note how "result" is ignored. 
                            allEntities.AddValue(st, entities);
                        } else {
                            /// This call can be very time-consuming
                            /// <see cref="FileCache.Instance"/> has made an attempt at explaining through logging, but it's <see cref="BaseCore.LogEvent"/> is most probably (as of Sep 2017) not subscribed to anyway.
                            s.Synchronize2(db, new API.Result()); // Note how we just discard interesting statistics now. 
                            synchronizerWasCalled = true;

                            /// Now ALL ENTITIES in this synchronizer-"universe" have been place into memory. 
                            /// In other words there is no more work that has to be done in this loop.
                            break;
                        }
                    }
                    if (synchronizerWasCalled) {
                        /// No need for calling <see cref="BaseSynchronizer.SynchronizeMapForeignKeys"/>, because was done as part of <see cref="BaseSynchronizer.Synchronize2"/>
                    } else {
                        s.SynchronizeMapForeignKeys(allEntities, new API.Result()); // Note how "result" is ignored. 
                    }

                    // Now all keys have been read. Before injection, mark as read (this will stop recursivity)
                    types.ForEach(st => {
                        _synchronizedTypesInternal.AddValue(st, true, () => "Expected all keys " + string.Join(", ", types.Select(temp => temp.ToStringVeryShort()) + " to be set at once, not " + st.ToStringVeryShort() + " to be set separately"));
                    });

                    /// Note how the injection process will make recursive calls to this method 
                    /// therefore we have now through <see cref="_synchronizedTypesInternal"/> opened up for these recursive calls to return immediately, 
                    /// while we are still locking out other threads until we are completely finished)

                    types.ForEach(st => {
                        /// TODO: THIS IS CALCULATED MULTIPLE TIMES (ALSO WITHIN <see cref="BaseSynchronizer.Inject"/>
                        var entities = EntityCache.Values.Where(e => st.IsAssignableFrom(e.GetType())).ToList(); // TODO: Add some more indexing within entityCache.
                        BaseInjector.CalculateExpansions(st, entities, db); /// Call this first, maybe some if these will also be percentile-evaluated and aggregated over foreign keys below.

                        BaseInjector.CalculateForeignKeyAggregates(st, entities, db); /// This will make recursive calls against this method (<see cref="GetMatchingEntities"/>)

                    });

                    /// TODO: Call all other Injector-classes relevant for this universe.
                    /// TODO: To be decided how to organise this. 
                    /// TODO: Store the types in Synchronizer maybe?
                    s.Inject(db); // TODO: Will search for entities like already done here!

                    types.ForEach(st => {
                        /// TODO: THIS IS CALCULATED MULTIPLE TIMES (ALSO WITHIN <see cref="BaseSynchronizer.Inject"/>
                        var entities = EntityCache.Values.Where(e => st.IsAssignableFrom(e.GetType())).ToList(); // TODO: Add some more indexing within entityCache.
                        // TOOD. Percentiles should most probably be called multiple times in a sort of iterative process.
                        // TODO: (both before and after injection)
                        BaseInjector.CalculatePercentiles(st, entities, db); // Call this last, so also foreign key aggregates AND injected values may be evaluated
                    });

                    return true; // Indicates that synchronizing has been done
                } // Release lock, other threads will now see a "complete" picture.
            });
            switch (id) {
                case QueryIdAll q:
                    return EntityCache.Values.Where(e => type.IsAssignableFrom(e.GetType())).ToList(); // TODO: Inefficient code, we could instead split cache into separate collections for each type without much trouble
                case QueryIdKeyOperatorValue q:
                    return EntityCache.Values.Where(e => { // TODO: Inefficient code, we could instead split cache into separate collections for each type without much trouble
                        if (!type.IsAssignableFrom(e.GetType())) return false;
                        return q.IsMatch(e);
                    }).ToList();
                case QueryIdMultiple q:
                    return EntityCache.Values.Where(e => { // TODO: Inefficient code, we could instead split cache into separate collections for each type without much trouble
                        if (!type.IsAssignableFrom(e.GetType())) return false;
                        return q.Ids.Any(i => i.IsMatch(e));
                    }).ToList();
                default:
                    throw new InvalidObjectTypeException(id);
            }
        }
    }

    public class InMemoryCacheException : ApplicationException {
        public InMemoryCacheException(string message) : base(message) { }
        public InMemoryCacheException(string message, Exception inner) : base(message, inner) { }
    }
}