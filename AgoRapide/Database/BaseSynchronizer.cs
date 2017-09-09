// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;
using System.Reflection;

namespace AgoRapide.Database {

    [Class(
        Description =
            "Synchronizes data from an external data source, usually with the goal of using AgoRapide to easily browse the data and generate reports.",
        LongDescription =
            "The data found is stored within -" + nameof(FileCache) + "-, " +
            "only identifiers (-" + nameof(PropertyKeyAttribute.ExternalPrimaryKeyOf) + "-) are stored within -" + nameof(BaseDatabase) + "-."
    )]
    public abstract class BaseSynchronizer : Agent {

        /// <summary>
        /// <see cref="CoreAPIMethod.BaseEntityMethod"/>. 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="request"></param>
        [APIMethod(
            Description = "Synchronizes from source",
            S1 = nameof(Synchronize), S2 = "DUMMY")] // TODO: REMOVE "DUMMY"
        public object Synchronize(BaseDatabase db, ValidRequest request) {
            var types = PV(SynchronizerP.SynchronizerExternalType.A(), defaultValue: new List<Type>());
            var entities = new Func<Dictionary<Type, List<BaseEntity>>>(() => {
                if (PV(SynchronizerP.SynchronizerUseMockData.A(), defaultValue: false)) { // Create mock-data.
                    // Note how this process is reproducable, the same result should be returned each time (given the same percentileValue)                    

                    // TOOD: ---------
                    // TODO: Add some functionality for configuring number of, and distribution of entities here.
                    var percentileValue = PV(SynchronizerP.SynchronizerMockSize.A(), defaultValue: new Percentile(3)).Value;
                    var defaultCount = percentileValue * percentileValue * percentileValue; // Default will be 27 entities. 
                    var maxN = types.ToDictionary(key => key, key => defaultCount);
                    // TOOD: ---------

                    return types.ToDictionary(t => t, t => GetMockEntities(t, new Func<PropertyKey, bool>(p => p.Key.A.IsExternal), maxN));
                }
                var retval = SynchronizeInternal(db, request.Result);
                if (retval.Count != types.Count) throw new InvalidCountException(retval.Count, types.Count,
                    "Found " + retval.KeysAsString() + ", expected " + string.Join(", ", types.Select(t => t.ToStringVeryShort())) + ".\r\n" +
                    "Resolution: Ensure that " + GetType() + "." + nameof(SynchronizeInternal) + " really returns data for all the types given in " + SynchronizerP.SynchronizerExternalType + ".");
                return retval;
            })();
            entities.ForEach(e => Reconcile(e.Key, e.Value, db, request.Result));
            // TODO: Add reconciling of all foreign keys here. 
            entities.ForEach(e => FileCache.Instance.StoreToDisk(this, e.Key, e.Value));

            AddProperty(SynchronizerP.SynchronizerDataHasBeenReadIntoMemoryCache.A(), true);

            request.Result.ResultCode = ResultCode.ok; /// It is difficult for sub class to set  <see cref="Result.ResultCode"/> because it does not know if it generated a complete result or was just called as part of something
            request.Result.AddProperty(CoreP.SuggestedUrl.A(), new Uri(request.API.CreateAPIUrl(this)));
            return request.GetResponse();
        }

        // TODO: REMOVE COMMENTED OUT CODE
        ///// <typeparam name="T">
        ///// This is mostly a hint about what needs to be synchronized. 
        ///// {BaseEntity} may be used, meaning implementator should synchronize all data. 
        ///// It is up to the synchronizer to do a fuller synchronization as deemed necessary or practical. 
        ///// </typeparam>

        public abstract Dictionary<Type, List<BaseEntity>> SynchronizeInternal(BaseDatabase db, Result result);

        /// <summary>
        /// Reconciles <paramref name="externalEntities"/> of type <paramref name="type"/> 
        /// found from external data source with what we already have stored in database. 
        /// 
        /// Primary keys are stored in database. 
        /// <paramref name="externalEntities"/> is enriched with information from the database. 
        /// 
        /// For entities in database for which the primary keys are no longer found, <see cref="PropertyOperation.SetInvalid"/> is performed. 
        /// 
        /// "Callback" from the implementation of <see cref="Synchronize{T}"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="externalEntities">
        /// This must be the complete list of all entities from external data source. 
        /// After the call is complete this list will have been enriched with information from database. 
        /// </param>
        /// <param name="db"></param>
        /// <param name="result"></param>
        private void Reconcile(Type type, List<BaseEntity> externalEntities, BaseDatabase db, Result result) { //  where T : BaseEntity, new() {
            InvalidTypeException.AssertAssignable(type, typeof(BaseEntity));
            // var type = typeof(T);

            /// Note how we cannot just do 
            ///   SetAndStoreCount(CountP.Total, externalEntities.Count, result, db);
            /// because that would specify neither <see cref="AggregationType"/> nor T (which is even more important, as we are called for different types, meaning value stored for last type would just be overridden)
            SetAndStoreCount(AggregationKey.GetAggregationKey(AggregationType.Count, type, CountP.Total.A()), externalEntities.Count, result, db);
            var primaryKey = type.GetChildProperties().Values.Single(k => k.Key.A.ExternalPrimaryKeyOf != null, () => nameof(PropertyKeyAttribute.ExternalPrimaryKeyOf) + " != null for " + type);

            /// TODO: Add some identificator here for "workspace" or similar.
            /// TODO: And call <see cref="BaseDatabase.TryGetEntities"/> instead with query like
            /// TOOD: WHERE workspace = {workspaceId}
            /// TODO: We could have workSpaceId as a property of ourselves. The id could be the same as 
            /// TODO: the <see cref="DBField.id"/> of a root administrative <see cref="Person"/> for our customer.
            var internalEntities = db.GetAllEntities(type);
            var internalEntitiesByPrimaryKey = internalEntities.ToDictionary(e => e.PV<long>(primaryKey), e => e);
            var newCount = 0;
            externalEntities.ForEach(e => { /// Reconcile through <see cref="PropertyKeyAttribute.ExternalPrimaryKeyOf"/>
                if (internalEntitiesByPrimaryKey.TryGetValue(e.PV<long>(primaryKey), out var internalEntity)) {
                    internalEntitiesByPrimaryKey.Remove(e.PV<long>(primaryKey)); // Remove in order to check at end for any left
                } else {
                    internalEntity = db.GetEntityById(db.CreateEntity(Id, type,
                        new List<(PropertyKeyWithIndex key, object value)> {
                            (primaryKey.PropertyKeyWithIndex, e.PV<long>(primaryKey))
                        }, result), type);
                    newCount++;
                }

                // Transfer from internal to external (assumed to constitute the least amount to transfer)
                e.Id = internalEntity.Id;
                internalEntity.Properties.Values.ForEach(p => {
                    if (p.Key.Key.A.IsExternal) {
                        /// Already contained in e. 
                        /// Would typically be <see cref="PropertyKeyAttribute.ExternalPrimaryKeyOf"/> with has to be stored in database anyway. 
                        /// Can also be the result of e being a cached entity (see caching below).
                        return;
                    }
                    e.AddProperty(p, detailer: null);
                });
                InMemoryCache.EntityCache[e.Id] = e; // Put into cache
            });
            SetAndStoreCount(AggregationKey.GetAggregationKey(AggregationType.Count, type, CountP.Created.A()), newCount, result, db);
            internalEntitiesByPrimaryKey.ForEach(e => { // Remove any internal entities left.
                db.OperateOnProperty(Id, e.Value.RootProperty, PropertyOperation.SetInvalid, result);
                if (InMemoryCache.EntityCache.ContainsKey(e.Value.Id)) InMemoryCache.EntityCache.TryRemove(e.Value.Id, out _);
            });
            SetAndStoreCount(AggregationKey.GetAggregationKey(AggregationType.Count, type, CountP.SetInvalid.A()), internalEntitiesByPrimaryKey.Count, result, db);

            // REMOVE COMMENTED OUT CODE. ForeignKey and storing is not done here.
            //// TODO: Map external foreign keys to internal ones.
            //// TODO: Decide where and when to do. Inside this method would be difficult for instance because as of Sep 2017 we do not have access to all entities.
            //// TODO: Most probably better to NOT store anything inside this method. 
            //// FileCache.Instance.StoreToDisk(this, externalEntities);
        }
    }

    [Enum(AgoRapideEnumType = EnumType.PropertyKey)]
    public enum SynchronizerP {
        None,

        [PropertyKey(
            Description = "Indicates that -" + nameof(BaseEntity.GetMockEntities) + "- shall be used by -" + nameof(BaseSynchronizer.Synchronize) + "-.",
            Type = typeof(bool),
            Parents = new Type[] { typeof(BaseSynchronizer) },
            AccessLevelRead = AccessLevel.Relation,
            AccessLevelWrite = AccessLevel.Relation
        )]
        SynchronizerUseMockData,

        [PropertyKey(
            Description =
                "Size of data set to be used for -" + nameof(SynchronizerUseMockData) + "-.",
            LongDescription =
                "-" + nameof(BaseSynchronizer) + "- will use this value's third power for default count of mock-entities of each type.",
            Type = typeof(Percentile),
            SampleValues = new string[] { "1P", "3P", "5P", "7P", "10P", "25P", "50P", "75P", "100P" },
            Parents = new Type[] { typeof(BaseSynchronizer) },
            AccessLevelRead = AccessLevel.Relation,
            AccessLevelWrite = AccessLevel.Relation
        )]
        SynchronizerMockSize,

        /// <summary>
        /// Note how this is usually communicated through <see cref="IStaticProperties"/>.
        /// </summary>
        [PropertyKey(
            Description = "-" + nameof(BaseEntity) + "--derived types that this synchronizer supports.",
            Type = typeof(Type),
            IsMany = true,
            Parents = new Type[] { typeof(BaseSynchronizer) },
            AccessLevelRead = AccessLevel.Relation
        )]
        SynchronizerExternalType,

        [PropertyKey(
            Description = "Last update against source.",
            Type = typeof(DateTime), DateTimeFormat = DateTimeFormat.DateHourMin,
            Parents = new Type[] { typeof(BaseSynchronizer) },
            AccessLevelRead = AccessLevel.Relation
        )]
        SynchronizerLastUpdateAgainstSource,

        /// <summary>
        /// TODO: Do we need this value? 
        /// </summary>
        [PropertyKey(
            Description = "TRUE if actual data has been read into -" + nameof(InMemoryCache) + "- after application startup.",
            Type = typeof(bool),
            Parents = new Type[] { typeof(BaseSynchronizer) },
            AccessLevelRead = AccessLevel.Relation
        )]
        SynchronizerDataHasBeenReadIntoMemoryCache,
    }

    public static class SynchronizerPExtensions {
        public static PropertyKey A(this SynchronizerP p) => PropertyKeyMapper.GetA(p);
    }
}