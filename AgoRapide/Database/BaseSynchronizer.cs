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

    /// <summary>
    /// 
    /// 
    /// 
    /// </summary>
    [Class(
        Description =
            "Synchronizes data from an external data source, usually with the goal of using AgoRapide to easily browse the data.",
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
            S1 = nameof(Synchronize), S2 = "DUMMY")]
        public object Synchronize(BaseDatabase db, ValidRequest request) {
            Synchronize<BaseEntity>(db, request.Result);
            request.Result.ResultCode = ResultCode.ok; /// It is difficult for sub class to set  <see cref="Result.ResultCode"/> because it does not know if it generated a complete result or was just called as part of something
            request.Result.AddProperty(CoreP.SuggestedUrl.A(), new Uri(request.API.CreateAPIUrl(this)));
            return request.GetResponse();
        }

        /// <summary>
        /// Synchronizes with source. 
        /// </summary>
        /// <typeparam name="T">
        /// This is mostly a hint about what needs to be synchronized. 
        /// {BaseEntity} may be used, meaning implementator should synchronize all data. 
        /// It is up to the synchronizer to do a fuller synchronization as deemed necessary or practical. 
        /// </typeparam>
        /// <param name="db"></param>
        /// <param name="fileCache"></param>
        [ClassMember(Description = "Synchronizes between local database / local file storage and external source")]
        public abstract void Synchronize<T>(BaseDatabase db, Result result) where T : BaseEntity, new();

        /// <summary>
        /// Reconciles data found from external data source with what we already have stored in database. 
        /// Primary keys are stored in database. 
        /// Entities in database with primary keys which are no longer found are <see cref="PropertyOperation.SetInvalid"/>. 
        /// 
        /// "Callback" from the implementation of <see cref="Synchronize{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="externalEntities">
        /// This must be the complete list of all entities from external data source. 
        /// </param>
        /// <param name="db"></param>
        /// <param name="result">
        /// TODO: Change this parameter to <see cref="Request"/>. 
        /// TODO: Aug 2017. Why?
        /// </param>
        protected void Reconcile<T>(List<T> externalEntities, BaseDatabase db, Result result) where T : BaseEntity, new() {
            var type = typeof(T);
            /// Note how we cannot just do 
            ///   SetAndStoreCount(CountP.Total, externalEntities.Count, result, db);
            /// because that would specify neither <see cref="AggregationType"/> nor T (which is even more important, as old value would just be overridden)
            SetAndStoreCount(AggregationKey.GetAggregationKey(AggregationType.Count, typeof(T), CountP.Total.A()), externalEntities.Count, result, db);
            var primaryKey = type.GetChildProperties().Values.Single(k => k.Key.A.ExternalPrimaryKeyOf != null, () => nameof(PropertyKeyAttribute.ExternalPrimaryKeyOf) + " != null for " + type);

            /// TODO: Add some identificator here for "workspace" or similar.
            /// TODO: And call <see cref="BaseDatabase.TryGetEntities"/> instead with query like
            /// TOOD: WHERE workspace = {workspaceId}
            /// TODO: We could have workSpaceId as a property of ourselves. The id could be the same as 
            /// TODO: the <see cref="DBField.id"/> of a root administrative <see cref="Person"/> for our customer.
            var internalEntities = db.GetAllEntities<T>();
            var internalEntitiesByPrimaryKey = internalEntities.ToDictionary(e => e.PV<long>(primaryKey), e => e);
            var newCount = 0;
            externalEntities.ForEach(e => { /// Reconcile through <see cref="PropertyKeyAttribute.ExternalPrimaryKeyOf"/>
                if (internalEntitiesByPrimaryKey.TryGetValue(e.PV<long>(primaryKey), out var internalEntity)) {
                    internalEntitiesByPrimaryKey.Remove(e.PV<long>(primaryKey)); // Remove in order to check at end for any left
                } else {
                    internalEntity = db.GetEntityById<T>(db.CreateEntity<T>(Id,
                        new List<(PropertyKeyWithIndex key, object value)> {
                            (primaryKey.PropertyKeyWithIndex, e.PV<long>(primaryKey))
                        }, result));
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
            SetAndStoreCount(AggregationKey.GetAggregationKey(AggregationType.Count, typeof(T), CountP.Created.A()), newCount, result, db);
            internalEntitiesByPrimaryKey.ForEach(e => { // Remove any internal entities left.
                db.OperateOnProperty(Id, e.Value.RootProperty, PropertyOperation.SetInvalid, result);
                if (InMemoryCache.EntityCache.ContainsKey(e.Value.Id)) InMemoryCache.EntityCache.TryRemove(e.Value.Id, out _);
            });
            SetAndStoreCount(AggregationKey.GetAggregationKey(AggregationType.Count, typeof(T), CountP.SetInvalid.A()), internalEntitiesByPrimaryKey.Count, result, db);

            FileCache.Instance.StoreToDisk(this, externalEntities);
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
                "Size of data set to be used for -" + nameof(SynchronizerUseMockData) + "-. " +
                "It is up to the implementation to interpret the value, typically as a -" + nameof(Percentile.Tertile) + "-",
            Type = typeof(Percentile),
            SampleValues = new string[] { "1P", "5P", "25P", "50P", "75P", "100P" },
            Parents = new Type[] { typeof(BaseSynchronizer) },
            AccessLevelRead = AccessLevel.Relation,
            AccessLevelWrite = AccessLevel.Relation
        )]
        SynchronizerMockSize,

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
            Type = typeof(DateTime), DateTimeFormat =DateTimeFormat.DateHourMin,
            Parents = new Type[] { typeof(BaseSynchronizer) },
            AccessLevelRead = AccessLevel.Relation
        )]
        SynchronizerLastUpdateAgainstSource,

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
