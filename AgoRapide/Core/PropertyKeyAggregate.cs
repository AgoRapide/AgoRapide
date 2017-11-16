// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {
    /// <summary>
    /// NOTE: <see cref="AggregationKey"/> is not to be confused with <see cref="PropertyKeyAggregate"/>
    /// NOTE: The former is more associate with API-operations, the latter is associated with data.
    /// TODO: The former should have a different name.
    /// 
    /// Created by <see cref="GetKeys"/> (called from <see cref="PropertyKeyMapper.MapEnumFinalize"/>), and by classes inheriting <see cref="BaseInjector"/>). 
    /// </summary>
    [Class(
        Description =
            "Represents aggregations sourced from all entities connected to a given entity " +
            "and stored as -" + nameof(PropertyKey) + "- for the given entity.",
        LongDescription = "Some examples of aggregations:\r\n" +
            "\r\n" +
            "1) -" + nameof(AggregationType.Count) + "-, -" + nameof(Person) + "-, PersonP.Count_Project_LeaderId " +
            "(based on foreignKey LeaderId in Projects)\r\n" +
            "Counting number of projects that a person is the leader of." +
            "\r\n" +
            "2) -" + nameof(AggregationType.Sum) + "-, -" + nameof(Person) + "-, PersonP.Sum_ProjectSumBudget " +
            "(based on foreignKey in Projects and Property.Budget-property with -" + nameof(PropertyKeyAttribute.AggregationTypes) + "- set to -" + nameof(AggregationType.Sum) + "-\r\n" +
            "Sum of all projects related to this person."
    )]
    public class PropertyKeyAggregate : PropertyKeyInjected {

        public AggregationType AggregationType { get; private set; }
        /// <summary>
        /// Must be specified since <see cref="SourceProperty"/> may have multiple values in <see cref="PropertyKeyAttribute.Parents"/>
        /// </summary>
        public Type SourceEntityType { get; private set; }

        [ClassMember(Description = "This is either identical to -" + nameof(ForeignKeyProperty) + "- or it can be any other property that can be aggregated.")]
        public PropertyKey SourceProperty { get; private set; }

        [ClassMember(Description = "The -" + nameof(PropertyKeyAttribute.ForeignKeyOf) + "--property of the aggregation source entity (linking the given entity and the source entity together).")]
        public PropertyKey ForeignKeyProperty { get; private set; }

        /// <summary>
        /// Should only be called at application startup through <see cref="PropertyKeyMapper"/>
        /// </summary>
        /// <param name="aggregationType"></param>
        /// <param name="sourceEntityType"></param>
        /// <param name="sourceProperty"></param>
        /// <param name="key"></param>
        public PropertyKeyAggregate(AggregationType aggregationType, Type sourceEntityType, PropertyKey foreignKeyProperty, PropertyKey sourceProperty, PropertyKeyAttributeEnriched key) : base(key) {
            Util.AssertCurrentlyStartingUp();
            AggregationType = aggregationType;
            SourceEntityType = sourceEntityType;
            ForeignKeyProperty = foreignKeyProperty;
            SourceProperty = sourceProperty;
        }

        /// <summary>
        /// Note that will also set <see cref="PropertyKeyAttribute.HasLimitedRange"/> as appropriate. 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="entities"></param>
        [ClassMember(
            Description = "Calculates the actual aggregates based on keys returned by -" + nameof(GetKeys) + "-.",
            LongDescription = "Example: If we have Persons and Projects and every Project has a foreign key LeaderPersonId, then this method will aggregate Count_ProjectLeaderPersonid for every Person.")]
        public static void CalculateValues(Type type, List<BaseEntity> entities) =>
            // Introduced Parallel.ForEach 3 Nov 2017
            Parallel.ForEach(type.GetChildProperties().Values.Select(key => key as PropertyKeyAggregate).Where(key => key != null), key => {
            var hasLimitedRange = true; var valuesFound = new HashSet<long>(); // TODO: Support other types of aggregations.

            /// Build index in order to avoid O(n^2) situation.
            var toEntitiesIndex = new Dictionary<long, List<BaseEntity>>();
            InMemoryCache.EntityCacheWhereIs(key.SourceEntityType).
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
                var sourceEntities = toEntitiesIndex.TryGetValue(e.Id, out var temp) ? temp : new List<BaseEntity>();
                
                long av; // Note how long is the preferred type for aggregations. 
                // TODO: Support other types of aggregations.
                if (key.ForeignKeyProperty.Key.CoreP == key.SourceProperty.Key.CoreP) {
                    if (key.AggregationType != AggregationType.Count) throw new InvalidEnumException(key.AggregationType, "Because " + nameof(key.ForeignKeyProperty));

                    av = sourceEntities.Count; 
                } else {
                    switch (key.AggregationType) {
                        case AggregationType.Sum:
                            InvalidTypeException.AssertEquals(key.Key.A.Type, typeof(long), () => key.ToString());
                            av = sourceEntities.Aggregate(0L, (acc, se) => acc + se.PV<long>(key.SourceProperty, 0)); break;
                        default: throw new NotImplementedException(key.SourceProperty.Key.PToString + " " + key.AggregationType + " for " + key.Key.A.Parents);
                    }
                }
                e.AddProperty(key, av);

                if (!valuesFound.Contains(av)) {
                    // TOOD: TURN LIMIT OF 30 INTO A CONFIGURATION-PARAMETER
                    // TODO: Or rather, create a LimitedRange-limit for PropertyKeyAttribute
                    if (valuesFound.Count >= 30) { // Note how we allow up to 30 DIFFERENT values, instead of values up to 20. This means that a distribution like 1,2,3,4,5,125,238,1048 still counts as limited.
                                                   // TODO: Or rather, create a LimitedRange-limit for PropertyKeyAttribute
                        hasLimitedRange = false;
                    } else {
                        valuesFound.Add(av);
                    }
                }
            });
            key.Key.A.HasLimitedRange = hasLimitedRange; /// If TRUE then important discovery making it possible for <see cref="Result.CreateDrillDownUrls"/> to make more suggestions.
        });

        /// <summary>
        /// Called from <see cref="PropertyKeyMapper.MapEnumFinalize"/>
        /// 
        /// Actual values are later calculated by <see cref="CalculateValues"/> (note how that one also sets <see cref="PropertyKeyAttribute.HasLimitedRange"/> as appropriate). 
        /// <param name="keys"></param>
        /// <returns></returns>
        [ClassMember(Description =
            "Generates -" + nameof(PropertyKeyAggregate) + "- for all aggregates that can automatically be deduced from standard AgoRapide information.\r\n" +
            "Will generate keys for -" + nameof(AggregationType.Count) + "- directly against foreign keys, and also keys for aggregates for all properties " +
            "for foreign entity which have -" + nameof(PropertyKeyAttribute.AggregationTypes) + "- set.\r\n")]
        public static List<PropertyKeyAggregate> GetKeys(List<PropertyKey> keys) {
            Util.AssertCurrentlyStartingUp();
            var retval = new List<PropertyKeyAggregate>();

            keys.Where(k => k.Key.A.ForeignKeyOf != null).ForEach(k => {
                k.Key.A.Parents.ForEach(p => { /// Note how multiple parents may share same foreign key. (Parents is guaranteed to be set now by <see cref="PropertyKeyAttributeEnriched.Initialize"/>.)
                                               /// IMPORTANT: DO NOT CALL p.GetChildProperties (That is, <see cref="Extensions.GetChildProperties"/> as value will be cached, making changes done her invisible)
                    keys.
                        Where(key => key.Key.HasParentOfType(p) && (key.Key.CoreP == k.Key.CoreP || (key.Key.A.AggregationTypes.Length > 0))).
                        ForEach(fp => { // Aggregate for all properties that are possible to aggregate over. 

                            // Added 14 Sep 2017. May have to relax a bit here though. 
                            InvalidTypeException.AssertEquals(fp.Key.A.Type, typeof(long), () => "Details: " + fp.ToString());
                            //if (fp.Key.PToString.Equals("VismaOrderLineValueWithCosts")) {
                            //    var a = 1;
                            //}
                            var aggregationTypes = fp.Key.A.AggregationTypes.ToList();
                            if (fp.Key.CoreP == k.Key.CoreP && !aggregationTypes.Contains(AggregationType.Count)) aggregationTypes.Add(AggregationType.Count); // Always count number of foreign entities
                            aggregationTypes.ForEach(a => {
                                var foreignKeyAggregateKey = new PropertyKeyAggregate(
                                    a,
                                    p,
                                    k,
                                    fp,
                                    new PropertyKeyAttributeEnrichedDyn(
                                        new PropertyKeyAttribute(
                                                // Note that if there is only ONE foreignKey pointing towards entity, then identification for the foreign key can be shortened further.
                                                // (we can have (for Person) Count_Project instead of Count_Project_PersonId for instance)
                                                property: (
                                                    k.Key.A.ForeignKeyOf.ToStringVeryShort() + "_" +
                                                    a + "_" +
                                                    p.ToStringVeryShort() + "_" + fp.Key.PToString.Replace("CorrespondingInternalKey", "")). // Note removal of "CorrespondingInternalKey", resulting in shorter identification                                           
                                                    Replace(a + "_" + p.ToStringVeryShort() + "_" + p.ToStringVeryShort(), a + "_" + p.ToStringVeryShort() + "_"), // This replace will turn for instance Count_Project_ProjectLeaderPersonId into Count_Project_LeaderPersonId, that is, it shortens down aggregate keys when the P-enums repeat their parent-entity type.
                                                description: "-" + k.Key.A.ForeignKeyOf.ToStringVeryShort() + "- -" + a + "- for -" + p.ToStringVeryShort() + "- -" + fp.Key.PToString.Replace("CorrespondingInternalKey", "") + "-." +
                                                    (a != AggregationType.Count ? "" : "\r\n(count of " + p.ToStringVeryShort() + " related to " + k.Key.A.ForeignKeyOf.ToStringVeryShort() + ".)"),
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
}