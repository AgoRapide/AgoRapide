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
            "Synchronizes data from external data storage " +
            "(for instance a CRM system from which the AgoRapide based application will analyze data).",
        LongDescription =
            "The data found is stored within -" + nameof(FileCache) + "-, " +
            "only identifiers (-" + nameof(PropertyKeyAttribute.ExternalPrimaryKeyOf) + "-) are stored within -" + nameof(BaseDatabase) + "-"
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
        /// "Callback" from the implementation of <see cref="Synchronize{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="externalEntities"></param>
        /// <param name="db"></param>
        /// <param name="result">
        /// TODO: Change this parameter to <see cref="Request"/>. 
        /// </param>
        protected void Reconcile<T>(List<T> externalEntities, BaseDatabase db, Result result) where T : BaseEntity, new() {
            var type = typeof(T);
            SetAndStoreCount(CountP.ETotalCount, externalEntities.Count, result, db);
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
            SetAndStoreCount(CountP.ETotalCount, externalEntities.Count, result, db);
            /// TODO: Add more advanced statistics counting here... 
            /// TODO: AND STORE ALSO AS PROPERTIES WITHIN Synchronizer permanently (not only as result)
            Log(type.ToString() + ", NewCount: " + newCount, result);
            /// TODO: Add more advanced statistics counting here... 
            /// TODO: AND STORE ALSO AS PROPERTIES WITHIN Synchronizer permanently (not only as result)
            Log(type.ToString() + ", DeletedCount: " + internalEntitiesByPrimaryKey.Count, result);
            internalEntitiesByPrimaryKey.ForEach(e => { // Remove any internal entities left.
                db.OperateOnProperty(Id, e.Value.RootProperty, PropertyOperation.SetInvalid, null);
                if (InMemoryCache.EntityCache.ContainsKey(e.Value.Id)) InMemoryCache.EntityCache.TryRemove(e.Value.Id, out _);
            });

            FileCache.Instance.StoreToDisk(externalEntities);
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
        UseMockData,

        [PropertyKey(
            Description =
                "Size of data set to be used for -" + nameof(UseMockData) + "-. " +
                "It is up to the implementation to interpret the value, typically as a -" + nameof(Percentile.Tertile) + "-",
            Type = typeof(Percentile),
            SampleValues = new string[] { "1P", "5P", "25P", "50P", "75P", "100P" },
            Parents = new Type[] { typeof(BaseSynchronizer) },
            AccessLevelRead = AccessLevel.Relation,
            AccessLevelWrite = AccessLevel.Relation
        )]
        MockSize,
    }

    public static class SynchronizerPExtensions {
        public static PropertyKey A(this SynchronizerP p) => PropertyKeyMapper.GetA(p);
    }

}
