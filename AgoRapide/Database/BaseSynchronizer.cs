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
            Synchronize2(db, request.Result);
            request.Result.AddProperty(CoreP.SuggestedUrl.A(), new Uri(request.API.CreateAPIUrl(this)));
            return request.GetResponse();
        }

        /// <summary>
        /// Split out from <see cref="Synchronize"/> in order to be able to call directly from <see cref="InMemoryCache"/>
        /// </summary>
        /// <param name="db"></param>
        /// <param name="result"></param>
        public void Synchronize2(BaseDatabase db, Result result) {
            var entities = SynchronizeGetEntities(db, result);
            entities.ForEach(e => SynchronizeReconcileWithDatabase(e.Key, e.Value, db, result));
            SynchronizeMapForeignKeys(entities);
            entities.ForEach(e => FileCache.Instance.StoreToDisk(this, e.Key, e.Value));
            AddProperty(SynchronizerP.SynchronizerDataHasBeenReadIntoMemoryCache.A(), true);
            result.ResultCode = ResultCode.ok;
        }

        private Dictionary<Type, List<BaseEntity>> SynchronizeGetEntities(BaseDatabase db, Result result) {
            var types = PV(SynchronizerP.SynchronizerExternalType.A(), defaultValue: new List<Type>());
            if (PV(SynchronizerP.SynchronizerUseMockData.A(), defaultValue: false)) { // Create mock-data.
                                                                                      // Note how this process is reproducable, the same result should be returned each time (given the same percentileValue)                    

                // TOOD: ---------
                // TODO: Add some functionality for configuring number of, and distribution of entities here.
                var percentileValue = PV(SynchronizerP.SynchronizerMockSize.A(), defaultValue: Percentile.Get(3)).Value;
                var defaultCount = percentileValue * percentileValue * percentileValue; // Default will be 27 entities. 
                var maxN = types.ToDictionary(key => key, key => defaultCount);
                // TOOD: ---------

                return types.ToDictionary(t => t, t => GetMockEntities(t, new Func<PropertyKey, bool>(p => p.Key.A.IsExternal), maxN));
            }
            var retval = SynchronizeInternal(db, result);
            if (retval.Count != types.Count) throw new InvalidCountException(retval.Count, types.Count,
                "Found " + retval.KeysAsString() + ", expected " + string.Join(", ", types.Select(t => t.ToStringVeryShort())) + ".\r\n" +
                "Resolution: Ensure that " + GetType() + "." + nameof(SynchronizeInternal) + " really returns data for all the types given in " + SynchronizerP.SynchronizerExternalType + ".");
            return retval;
        }

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
        private void SynchronizeReconcileWithDatabase(Type type, List<BaseEntity> externalEntities, BaseDatabase db, Result result) { 
            InvalidTypeException.AssertAssignable(type, typeof(BaseEntity));

            /// Note how we cannot just do 
            ///   SetAndStoreCount(CountP.Total, externalEntities.Count, result, db);
            /// because that would specify neither <see cref="AggregationType"/> nor T (which is even more important, as we are called for different types, meaning value stored for last type would just be overridden)
            SetAndStoreCount(AggregationKey.Get(AggregationType.Count, type, CountP.Total.A()), externalEntities.Count, result, db);
            var primaryKey = type.GetChildProperties().Values.Single(k => k.Key.A.ExternalPrimaryKeyOf != null, () => nameof(PropertyKeyAttribute.ExternalPrimaryKeyOf) + " != null for " + type);

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
            SetAndStoreCount(AggregationKey.Get(AggregationType.Count, type, CountP.Created.A()), newCount, result, db);
            internalEntitiesByPrimaryKey.ForEach(e => { // Remove any internal entities left.
                db.OperateOnProperty(Id, e.Value.RootProperty, PropertyOperation.SetInvalid, result);
                if (InMemoryCache.EntityCache.ContainsKey(e.Value.Id)) InMemoryCache.EntityCache.TryRemove(e.Value.Id, out _);
            });
            SetAndStoreCount(AggregationKey.Get(AggregationType.Count, type, CountP.SetInvalid.A()), internalEntitiesByPrimaryKey.Count, result, db);
        }

        /// <summary>
        /// Adds corresponding properties for <see cref="DBField.id"/> values of foreign keys
        /// 
        /// (Necessary for instance for <see cref="Context"/> to be able to traverse relations).
        /// 
        /// Depends on every <see cref="PropertyKeyAttribute.ExternalForeignKeyOf"/> having a corresponding <see cref="PropertyKeyAttribute.ForeignKey"/>
        /// </summary>
        /// <param name="entities"></param>
        public void SynchronizeMapForeignKeys(Dictionary<Type, List<BaseEntity>> entities) {
            var foreignPrimaryKeyToPrimaryKeyIndexes = new Dictionary<Type, Dictionary<object, long>>();
            entities.ForEach(e => {
                e.Key.GetChildProperties().Values.Where(p => p.Key.A.ExternalForeignKeyOf != null).ForEach(p => {
                    var foreignType = p.Key.A.ExternalForeignKeyOf;
                    var correspondingInternalKey = e.Key.GetChildProperties().Values.Single(p2 => p2.Key.PToString == p.Key.PToString + "CorrespondingInternalKey", () => p.Key.PToString + "CorrespondingInternalKey for " + e.Key.ToStringVeryShort());
                    Dictionary <object, long> indexesThisForeignKeyType = null;
                    e.Value.ForEach(entity => {
                        if (!entity.Properties.TryGetValue(p.Key.CoreP, out var fk)) return;
                        if (indexesThisForeignKeyType == null) { 
                            if (!foreignPrimaryKeyToPrimaryKeyIndexes.TryGetValue(foreignType, out indexesThisForeignKeyType)) { // Note how we ensure that we build index for a specific foreignType only when needed and only once, even if referred from different properties.
                                var externalPrimaryKeyOfForeignType = foreignType.GetChildProperties().Values.Single(k => k.Key.A.ExternalPrimaryKeyOf != null, () => nameof(PropertyKeyAttribute.ExternalPrimaryKeyOf) + " of " + foreignType);
                                indexesThisForeignKeyType = (foreignPrimaryKeyToPrimaryKeyIndexes[foreignType] = // Construct index                                    
                                    entities.GetValue(foreignType, () => "Possible resolution: Add " + foreignType + " to " + SynchronizerP.SynchronizerExternalType).ToDictionary(
                                        entityOfForeignType => entityOfForeignType.Properties.GetValue(externalPrimaryKeyOfForeignType.Key.CoreP, () => externalPrimaryKeyOfForeignType.ToString() + " not found for " + entityOfForeignType.ToString()).Value,
                                        entityOfForeignType => entityOfForeignType.Id
                                    ));
                            }
                        }
                        entity.Properties.AddValue(correspondingInternalKey.Key.CoreP, new PropertyT<long>(correspondingInternalKey.PropertyKeyWithIndex, indexesThisForeignKeyType.GetValue(fk.Value, () => p.Key.PToString + " -" + fk.Value + "- for " + entity.ToString())));
                    });
                });
            });
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