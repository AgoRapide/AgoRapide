using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using System.Reflection;
using AgoRapide.API;

/// <summary>
/// Note: <see cref="AgoRapide.Database.ForeignKeyAggregateKey"/> also resides in this file for the time being (see below). 
/// </summary>
namespace AgoRapide.Database {

    /// <summary>
    /// Note <see cref="BaseInjector"/> assumes <see cref="CacheUse.All"/> for all entities involved and therefore always queries <see cref="InMemoryCache"/> directly.
    /// 
    /// TODO: Class should probably be renamed into ForeignKeyAggregatesInjecter if all other injectors are stored within <see cref="BaseSynchronizer"/>. 
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

        /// <summary>
        /// Note that will also set <see cref="PropertyKeyAttribute.HasLimitedRange"/> as appropriate. 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="entities"></param>
        /// <param name="db"></param>
        [ClassMember(Description = "Calculates the actual aggregates based on keys returned by -" + nameof(GetForeignKeyAggregateKeys) + "-.")]
        public static void CalculateForeignKeyAggregates(Type type, List<BaseEntity> entities, BaseDatabase db) => type.GetChildProperties().Values.Select(key => key as ForeignKeyAggregateKey).Where(key => key != null).ForEach(key => {
            var hasLimitedRange = true; var valuesFound = new HashSet<long>();

            // Akkurat samme problem her med index!
            /// Build index in order to avoid O(n^2) situation.
            var toEntitiesIndex = new Dictionary<long, List<BaseEntity>>();
            InMemoryCache.EntityCache.Values.
                Where(e => key.SourceEntityType.IsAssignableFrom(e.GetType())). /// TODO: Index entities by type in entity cache, in order no to repeat queries like this:
                ForEach(e => { /// TODO: Consider implementing indices like this in <see cref="InMemoryCache"/>
                    if (e.Properties.TryGetValue(key.ForeignKeyProperty.Key.CoreP, out var p)) {
                        var foreignKeyValue = p.V<long>();
                        if (!toEntitiesIndex.TryGetValue(foreignKeyValue, out var list)) list = (toEntitiesIndex[foreignKeyValue] = new List<BaseEntity>());
                        list.Add(e);
                    }
                });

            entities.ForEach(e => {
                InvalidObjectTypeException.AssertAssignable(e, type);
                // TODO: Note potential repeated calculations of sourceEntities here. We could have used LINQ GroupBy, but would then have to
                // TODO: take into account that the ForeignKeyProperty may actually also vary, not only the SourceEntityType

                // Unuseable because O(n^2)
                // var sourceEntities = InMemoryCache.GetMatchingEntities(key.SourceEntityType, new QueryIdKeyOperatorValue(key.ForeignKeyProperty.Key, Operator.EQ, e.Id), db);

                // New variant with index created above
                var sourceEntities = toEntitiesIndex.TryGetValue(e.Id, out var temp) ? temp : new List<BaseEntity>();

                if (key.ForeignKeyProperty.Key.CoreP == key.SourceProperty.Key.CoreP) {
                    if (key.AggregationType != AggregationType.Count) throw new InvalidEnumException(key.AggregationType, "Because " + nameof(key.ForeignKeyProperty));

                    var av = (long)sourceEntities.Count;
                    e.AddProperty(key, av); // Note cast since long is the preferred type for aggregations. 

                    if (!valuesFound.Contains(av)) {
                        if (valuesFound.Count >= 20) { // Note how we allow up to 20 DIFFERENT values, instead of values up to 20. This means that a distribution like 1,2,3,4,5,125,238,1048 still counts as limited.
                            hasLimitedRange = false;
                        } else {
                            valuesFound.Add(av);
                        }
                    }
                } else {
                    throw new NotImplementedException(key.SourceProperty.Key.PToString);
                }
            });
            key.Key.A.HasLimitedRange = hasLimitedRange; /// If TRUE then important discovery making it possible for <see cref="Result.CreateDrillDownUrls"/> to make more suggestions.
        });

        /// <summary>
        /// To be be done single threaded at application startup. 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="corePGetter"></param>
        /// <returns></returns>
        [ClassMember(Description =
            "Generates -" + nameof(ForeignKeyAggregateKey) + "- for all aggregates that can automatically be deduced from standard AgoRapide information.\r\n" +
            "Will generate -" + nameof(AggregationType.Count) + "- directly against foreign keys, and also aggregates for all properties " +
            "foreign entity which have -" + nameof(PropertyKeyAttribute.AggregationTypes) + "- set.\r\n" +
            "Called from -" + nameof(PropertyKeyMapper.MapEnumFinalize) + "-.")]
        public static List<ForeignKeyAggregateKey> GetForeignKeyAggregateKeys(List<PropertyKey> keys) {
            var retval = new List<ForeignKeyAggregateKey>();

            keys.Where(k => k.Key.A.ForeignKeyOf != null).ForEach(k => {
                k.Key.A.Parents.ForEach(p => { /// Note how multiple parents may share same foreign key. (Parents is guaranteed to be set now by <see cref="PropertyKeyAttributeEnriched.Initialize"/>.)
                                               /// IMPORTANT: DO NOT CALL p.GetChildProperties (That is, <see cref="Extensions.GetChildProperties"/> as value will be cached, making changes done her invisible)
                    keys.
                        Where(key => key.Key.IsParentFor(p) && (key.Key.CoreP == k.Key.CoreP || (key.Key.A.AggregationTypes != null && key.Key.A.AggregationTypes.Length > 0))).
                        ForEach(fp => { // Aggregate for all properties that are possible to aggregate over. 

                            // Added 14 Sep 2017. May have to relax a bit here though. 
                            InvalidTypeException.AssertEquals(fp.Key.A.Type, typeof(long), () => "Details: " + fp.ToString());

                            var aggregationTypes = k.Key.A.AggregationTypes.ToList();
                            if (fp.Key.CoreP == k.Key.CoreP && !aggregationTypes.Contains(AggregationType.Count)) aggregationTypes.Add(AggregationType.Count); // Always count number of foreign entities
                            aggregationTypes.ForEach(a => {
                                var foreignKeyAggregateKey = new ForeignKeyAggregateKey(
                                    a,
                                    p,
                                    k,
                                    fp,
                                    new PropertyKeyAttributeEnrichedDyn(
                                        new PropertyKeyAttribute(

                                            // TODO: REMOVE COMMENTED OUT CODE. Not possible if entity is foreignKey multiple times (project leader and project secretary for instance)
                                            // Note use of "CorrespondingInternalKey", resulting in shorter identification where we only want to count the number of gateways
                                            //property: k.Key.A.ForeignKeyOf.ToStringVeryShort() + "_" + a + "_" + p.ToStringVeryShort() + (fp.Key.PToString.EndsWith("CorrespondingInternalKey") ? "" : ("_" + fp.Key.PToString)),
                                            //description: "-" + k.Key.A.ForeignKeyOf.ToStringVeryShort() + "- -" + a + "- for -" + p.ToStringVeryShort() + "-" + (fp.Key.PToString.EndsWith("CorrespondingInternalKey") ? "" : (" - " + fp.Key.PToString + "-")),

                                            // Note removal of "CorrespondingInternalKey", resulting in shorter identification
                                            // Note that if there is only ONE foreignKey pointing towards entity, then identification for the foreign key can be shortened further. 
                                            property: k.Key.A.ForeignKeyOf.ToStringVeryShort() + "_" + a + "_" + p.ToStringVeryShort() + "_" + fp.Key.PToString.Replace("CorrespondingInternalKey", ""),
                                                description: "-" + k.Key.A.ForeignKeyOf.ToStringVeryShort() + "- -" + a + "- for -" + p.ToStringVeryShort() + "- -" + fp.Key.PToString.Replace("CorrespondingInternalKey", "") + "-",
                                                longDescription: "",
                                                isMany: false
                                            ) {
                                            Parents = new Type[] { k.Key.A.ForeignKeyOf },
                                            Type = typeof(long), /// TODO: Maybe allow double also for aggregations?

                                            /// TODO: Note how <see cref="BaseEntity.ToHTMLTableRowHeading"/> / <see cref="BaseEntity.ToHTMLTableRow"/> uses
                                            /// TODO: <see cref="Extensions.GetChildPropertiesByPriority(Type, PriorityOrder)"/> which as of Sep 2017
                                            /// TODO: will not take into count access level as set here.
                                            /// TOOD: (while <see cref="BaseEntity.ToHTMLDetailed"/> uses <see cref="Extensions.GetChildPropertiesForUser"/>
                                            AccessLevelRead = AccessLevel.Relation // Important, make visible to user
                                        },
                                        (CoreP)PropertyKeyMapper.GetNextCorePId()
                                    )
                                );
                                foreignKeyAggregateKey.SetPropertyKeyWithIndexAndPropertyKeyAsIsManyParentOrTemplate(); // HACK!    
                                retval.Add(foreignKeyAggregateKey);
                            });
                        });
                });
            });
            return retval;
        }
    }

    /// <summary>
    /// TODO: Move to separate file.
    /// 
    /// Represents aggregations sourced from all entities connected to a given entity and stored as <see cref="PropertyKey.Key"/> for the given entity. 
    /// 
    /// Created by <see cref="BaseInjector"/> (called from <see cref="PropertyKeyMapper.MapEnumFinalize"/>), and by classes inheriting <see cref="BaseInjector"/>. 
    ///
    /// Some examples of aggregations:
    /// 
    /// 1) <see cref="AggregationType.Count"/>, <see cref="Person"/>, PersonP.Count_Project_LeaderId (based on foreignKey LeaderId in Projects)
    ///    Counting number of projects that a person is the leader of.
    ///
    /// 2) <see cref="AggregationType.Sum"/>, <see cref="Person"/>, PersonP.Sum_ProjectSumBudget (based on foreignKey in Projects and Property.Budget-property)
    ///    Sum of all projects related to this person.
    ///    
    /// NOTE: <see cref="AggregationKey"/> is not to be confused with <see cref="ForeignKeyAggregateKey"/>
    /// </summary>
    public class ForeignKeyAggregateKey : PropertyKey {

        public AggregationType AggregationType { get; private set; }
        /// <summary>
        /// Must be specified since <see cref="SourceProperty"/> may have multiple values in <see cref="PropertyKeyAttribute.Parents"/>
        /// </summary>
        public Type SourceEntityType { get; private set; }

        /// <summary>
        /// The <see cref="PropertyKeyAttribute.ForeignKeyOf"/>-property of the aggregation source entity (linking the given entity and the source entity together)
        /// </summary>
        public PropertyKey ForeignKeyProperty { get; private set; }

        /// <summary>
        /// This is either identical to <see cref="ForeignKeyProperty"/> or it can be any other property that can be aggregated. 
        /// </summary>
        public PropertyKey SourceProperty { get; private set; }

        ///// <summary>
        ///// Private in order to limit number of possible objects created (by going through cache with <see cref="Get"/>. 
        ///// </summary>
        ///// <param name="key"></param>

        /// <summary>
        /// Should only be called at application startup through <see cref="PropertyKeyMapper"/>
        /// </summary>
        /// <param name="aggregationType"></param>
        /// <param name="sourceEntityType"></param>
        /// <param name="sourceProperty"></param>
        /// <param name="key"></param>
        public ForeignKeyAggregateKey(AggregationType aggregationType, Type sourceEntityType, PropertyKey foreignKeyProperty, PropertyKey sourceProperty, PropertyKeyAttributeEnriched key) : base(key) {
            AggregationType = aggregationType;
            SourceEntityType = sourceEntityType;
            ForeignKeyProperty = foreignKeyProperty;
            SourceProperty = sourceProperty;
        }
    }
}