﻿// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
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

        private static ConcurrentDictionary<Type, object> _entityCacheWhereIsCacheT = new ConcurrentDictionary<Type, object>();
        public static List<T> EntityCacheWhereIs<T>() => (List<T>)(_entityCacheWhereIsCacheT.GetOrAdd(typeof(T), t => EntityCacheWhereIs(typeof(T)).Select(e => (T)(object)e).ToList()));
        private static ConcurrentDictionary<Type, List<BaseEntity>> _entityCacheWhereIsCacheType = new ConcurrentDictionary<Type, List<BaseEntity>>();
        public static List<BaseEntity> EntityCacheWhereIs(Type type) => _entityCacheWhereIsCacheType.GetOrAdd(type, t => EntityCache.Values.Where(e => type.IsAssignableFrom(e.GetType())).ToList());

        /// <summary>
        /// Usually reset is done as a precaution when exceptions occur. 
        /// 
        /// ResetEntityCache removed from code 28 Sep 2017 because does not work well with <see cref="BaseSynchronizer"/> / <see cref="CacheUse.All"/>
        /// </summary>
        public static void ResetEntityCache() => throw new NotImplementedException(); //  EntityCache = new ConcurrentDictionary<long, BaseEntity>();

        private static List<Type> _synchronizerTypes = new List<Type>();
        /// <summary>
        /// Call this method for every <see cref="BaseSynchronizer"/>-type that your application uses. 
        /// Not thread-safe. Only to be used by single thread at application initialization. 
        /// </summary>
        /// <param name="synchronizer"></param>
        public static void AddSynchronizerType(Type synchronizer) {
            Util.AssertCurrentlyStartingUp();
            InvalidTypeException.AssertAssignable(synchronizer, typeof(BaseSynchronizer));
            _synchronizerTypes.Add(synchronizer);
        }

        private static ConcurrentDictionary<
            Type,
            /// TRUE means <see cref="BaseSynchronizer"/> exists, FALSE means no <see cref="BaseSynchronizer"/> exists.
            /// TODO: Note how this is (as of Sep 2017) decided only once in application lifetime
            /// TODO: Fix so that can add <see cref="BaseSynchronizer"/> later within application lifetime.
            bool
        > _synchronizedTypes = new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// Used inside GetOrAdd for <see cref="_synchronizedTypes"/>
        /// </summary>
        private static ConcurrentDictionary<Type, bool> _synchronizedTypesInternal = new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// Returns all entities of <paramref name="type"/> matching <paramref name="id"/>
        /// 
        /// Use this for <see cref="CacheUse.All"/>
        /// 
        /// Calls <see cref="FileCache.TryEnrichFromDisk"/> and also <see cref="BaseSynchronizer.Synchronize"/> as needed.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="db"></param>
        /// <param name="logger">
        /// May be null. 
        /// Useful of some operations from inside this method takes a very long time, 
        /// like accessing a <see cref="BaseSynchronizer"/>-class or the <see cref="FileCache.Instance"/></param>
        /// <returns></returns>
        public static List<BaseEntity> GetMatchingEntities(Type type, QueryId id, BaseDatabase db, Action<string> logger) {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (db == null) throw new ArgumentNullException(nameof(db));

            // Note that other calls may be made "simultaneously" (on other threads) to this metod, either for same type or for
            // another type within the same "synchronizer universe". 
            _synchronizedTypes.GetOrAdd(type, t => { // Note how actual return value, TRUE / FALSE, is ignored.
                if (!GetSynchronizers(db).TryGetValue(t, out var s)) {
                    /// This is either because 
                    /// 1) Synchronizing is not relevant for this type, or
                    /// 2) No synchronizers has been created in the database yet (note <see cref="ResetForType(Type)"/> which corrects against this (usually after first synchronization has been done)). 
                    /// 3) OR (IMPORTANT) You may just have forgotten to call <see cref="AddSynchronizerType"/> from your Startup.cs
                    return false;
                }
                lock (_synchronizedTypesInternal) { // Ensure that this "universe" completes first
                    if (_synchronizedTypesInternal.TryGetValue(t, out _)) return true; // Already done by other call (possible on other thread) within same universe

                    var types = s.PV<List<Type>>(SynchronizerP.SynchronizerExternalType.A());
                    var allEntities = new ConcurrentDictionary<Type, List<BaseEntity>>();

                    var Log = new Action<string>(text => {
                        if (logger == null) return;
                        logger(typeof(InMemoryCache).ToStringVeryShort() + "." + nameof(GetMatchingEntities) + ": " + text);
                    });

                    var tryEnrichFromDiskReturnedFalse = false;
                    Parallel.ForEach(types, (st, state) => { // NOTE: Parallelization is considered very relevant here because reading from disk entails a lot of in-memory processing (that is, this process is CPU-bound, not disk-bound).
                        if (state.IsStopped) return; /// This check / signal is almost meaningless for only a few types (more threads than types), because <see cref="FileCache.TryEnrichFromDisk"/> is what is time consuming.
                        Log("Calling " + nameof(FileCache.TryEnrichFromDisk) + " for " + st.ToStringVeryShort());
                        if (FileCache.Instance.TryEnrichFromDisk(s, st, db, out var entities, out _)) { // Note how "result" is ignored. 
                            allEntities.AddValue(st, entities);
                        } else {
                            state.Stop(); /// This check / signal is almost meaningless for only a few types (more threads than types), because <see cref="FileCache.TryEnrichFromDisk"/> is what is time consuming.
                            tryEnrichFromDiskReturnedFalse = true;
                        }
                    });

                    if (!tryEnrichFromDiskReturnedFalse) {
                        Log("Calling " + nameof(BaseSynchronizer.SynchronizeMapForeignKeys) + " for " + s.IdFriendly);
                        s.SynchronizeMapForeignKeys(allEntities, new API.Result()); // Note how "result" is ignored. 
                    } else {
                        /// Note that <see cref="BaseSynchronizer.SynchronizeMapForeignKeys"/> will be called as part of <see cref="BaseSynchronizer.Synchronize2"/>
                        /// 
                        /// This call can be very time-consuming
                        /// <see cref="FileCache.Instance"/> has made an attempt at explaining through logging, but it's <see cref="BaseCore.LogEvent"/> is most probably (as of Sep 2017) not subscribed to anyway.
                        Log("Calling " + nameof(BaseSynchronizer.Synchronize2) + " for " + s.IdFriendly);
                        s.Synchronize2(db, new API.Result()); // Note how we just discard interesting statistics now. 
                        /// Now ALL ENTITIES in this synchronizer-"universe" have been place into memory. 
                    }

                    /// Now all keys have been read. 
                    /// Before injection below, mark as read 
                    /// (this will stop recursivity that would otherwise occur because injecting below will call <see cref="GetMatchingEntities"/>)
                    types.ForEach(st => {
                        _synchronizedTypesInternal.AddValue(st, true, () => "Expected all keys " + string.Join(", ", types.Select(temp => temp.ToStringVeryShort()) + " to be set at once, not " + st.ToStringVeryShort() + " to be set separately"));
                    });

                    /// Note how the injection process will make recursive calls to this method 
                    /// therefore we have now through <see cref="_synchronizedTypesInternal"/> opened up for these recursive calls to return immediately, 
                    /// while we are still locking out other threads until we are completely finished)

                    /// TODO: Move this code into <see cref="BaseInjector"/>
                    types.ForEach(st => {
                        var entities = EntityCacheWhereIs(st);

                        Log("Calling " + nameof(PropertyKeyExpansion) + "." + nameof(PropertyKeyExpansion.CalculateValues) + " for " + st.ToStringVeryShort());
                        PropertyKeyExpansion.CalculateValues(st, entities); /// Call this first, maybe some if these will also be percentile-evaluated and aggregated over foreign keys below.

                        Log("Calling " + nameof(PropertyKeyAggregate) + "." + nameof(PropertyKeyAggregate.CalculateValues) + " for " + st.ToStringVeryShort());
                        PropertyKeyAggregate.CalculateValues(st, entities);

                        Log("Calling " + nameof(PropertyKeyJoinTo) + "." + nameof(PropertyKeyJoinTo.CalculateValues) + " for " + st.ToStringVeryShort());
                        PropertyKeyJoinTo.CalculateValues(st, entities);
                    });

                    /// TODO: Move this code into <see cref="BaseInjector"/>

                    /// TODO: Call all other Injector-classes relevant for this universe.
                    /// TODO: To be decided how to organise this. 
                    /// TODO: Store the types in Synchronizer maybe?
                    Log("Calling " + nameof(BaseSynchronizer.Inject) + " for " + s.IdFriendly);
                    s.Inject(db); // TODO: Will search for entities like already done here!

                    /// TODO: Move this code into <see cref="BaseInjector"/>

                    types.ForEach(st => {
                        var entities = EntityCacheWhereIs(st);

                        // TOOD. Percentiles should most probably be called multiple times in a sort of iterative process.
                        // TODO: (both before and after injection)

                        Log("Calling " + nameof(Percentile.Calculate) + " for " + st.ToStringVeryShort());

                        Percentile.Calculate(st, entities, db); // Call this last, so also foreign key aggregates AND injected values may be evaluated
                    });
                    Log("Finished");

                    return true; // Indicates that synchronizing has been done
                } // Release lock, other threads will now see a "complete" picture.
            });
            switch (id) {
                case QueryIdAll q:
                    return EntityCacheWhereIs(type);
                case QueryIdKeyOperatorValue q:
                    return EntityCacheWhereIs(type).Where(e => q.IsMatch(e)).ToList();
                case QueryIdMultiple q:
                    return EntityCacheWhereIs(type).Where(e => q.Ids.Any(i => i.IsMatch(e))).ToList();
                default:
                    throw new InvalidObjectTypeException(id);
            }
        }

        private static ConcurrentDictionary<Type, BaseSynchronizer> _synchronizersByType;
        /// <summary>
        /// TODO: Note some unresolved issues here. 
        /// TODO: Should for instance a given type be restricted to only one <see cref="BaseSynchronizer"/> reading that type?
        /// TODO: (see note in exception message below)
        /// TODO: Note that improvements are assumed to be possible to solve internally within <see cref="InMemoryCache"/>, that is without changing the external interface. 
        /// </summary>
        /// <param name="db">
        /// Only used at first call. Ignored for subsequent calls.
        /// </param>
        /// <returns></returns>
        [ClassMember(Description = "Returns (cached) information about which -" + nameof(BaseSynchronizer) + "--instance to use for which type.")]
        private static ConcurrentDictionary<Type, BaseSynchronizer> GetSynchronizers(BaseDatabase db) => _synchronizersByType ?? (_synchronizersByType = new Func<ConcurrentDictionary<Type, BaseSynchronizer>>(() => {
            var retval = new ConcurrentDictionary<Type, BaseSynchronizer>();
            if (_synchronizerTypes.Count == 0) throw new InMemoryCacheException("No calls made to -" + nameof(AddSynchronizerType) + "-. Should have been done at application startup.");
            _synchronizerTypes.ForEach(t => {
                db.GetAllEntities(t).ForEach(s => {
                    s.PV(SynchronizerP.SynchronizerExternalType.A(), new List<Type>()).ForEach(et => {
                        if (retval.TryGetValue(et, out var duplicate)) { /// Note how this problem is only caught at initial call to <see cref="GetSynchronizers"/>
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

        [ClassMember(Description = 
            "Resets information about given type.\r\n" +
            "Would typically be called from -" + nameof(BaseSynchronizer.Synchronize) + "-.\r\n" +
            "Calling this method forces for instance -" + nameof(FileCache.TryEnrichFromDisk) + "- to be called the next time when for instance -" + nameof(GetMatchingEntities) + "- is called.")]
        public static void ResetForType(Type type) {
            EntityCache.Where(e => type.IsAssignableFrom(e.GetType())).ForEach(e => EntityCache.TryRemove(e.Key, out _));
            // TODO: Work more on correct order of execution here in multi-thread scenarios.
            _entityCacheWhereIsCacheType.TryRemove(type, out _);
            _entityCacheWhereIsCacheT.TryRemove(type, out _);

            /// Try to catch situations where synchronizer was added to database after last call to <see cref="GetSynchronizers"/> 
            /// NOTE: Ideally this method should be called as soon as synchronizer was created, but as of Oct 2017 the call is done
            /// NOTE: by <see cref="BaseSynchronizer.Synchronize"/>            
            /// 
            /// NOTE: This would not be correct
            /// _synchronizersByType?.TryRemove(type, out _);
            /// This is the correct approach.
            _synchronizersByType = null;

            _synchronizedTypesInternal.TryRemove(type, out _);
            _synchronizedTypes.TryRemove(type, out _);
        }
    }

    public class InMemoryCacheException : ApplicationException {
        public InMemoryCacheException(string message) : base(message) { }
        public InMemoryCacheException(string message, Exception inner) : base(message, inner) { }
    }
}