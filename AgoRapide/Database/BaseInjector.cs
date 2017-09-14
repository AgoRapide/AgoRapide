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

    [Class(
        Description =
            "Responsible for injecting values that are only to be stored dynamically in RAM.\r\n" +
            "That is, values that are not to be stored in database, neither to be synchronized from external source.",
        LongDescription =
            nameof(BaseInjector) + " injects values that can automatically be deduced from standard AgoRapide information\r\n" +
            "(a typical example would be aggregations over foreign keys).\r\n" +
            "Inherited classes inject additional values based on their own C# logic")]
    public class BaseInjector {

        public static void GenerateAggregates(Type type, List<BaseEntity> entities) {
            type.GetChildProperties().Values.Select(key => key as ForeignKeyAggregateKey).Where(key => key != null).ForEach(key => {

            });
        }

        /// <summary>
        /// TODO: 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="corePGetter"></param>
        /// <returns></returns>
        [ClassMember(Description =
            "Generates all aggregates that can automatically be deduced from standard AgoRapide information.\r\n" +
            "Will generate -" + nameof(AggregationType.Count) + "- directly against foreign keys, and also aggregates for all properties " +
            "foreign entity which have -" + nameof(PropertyKeyAttribute.AggregationTypes) + "- set.\r\n" +
            "Called from -" + nameof(PropertyKeyMapper.MapEnumFinalize) + "-.")]
        public static List<ForeignKeyAggregateKey> GetForeignKeyAggregates(List<PropertyKey> keys) {
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
                                retval.Add(new ForeignKeyAggregateKey(
                                    a,
                                    p,
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
                                            Type = typeof(long) /// TODO: Maybe allow double also for aggregations?
                                        },
                                            (CoreP)PropertyKeyMapper.GetNextCorePId()
                                        )
                                    ));
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
    /// Created by <see cref="BaseInjector"/> (called from <see cref="PropertyKeyMapper.MapEnumFinalize"/>), and by classes inheriting <see cref="BaseInjector"/>. 
    ///
    /// Some examples of aggregations:
    /// 
    /// 1) <see cref="AggregationType.Count"/>, <see cref="Person"/>, PersonP.ProjectCount (based on foreignKey in Projects)
    ///    Counting number of foreign entities. 
    ///
    /// 2) <see cref="AggregationType.Count"/>, <see cref="Person"/>, PersonP.ProjectCount (based on foreignKey in Projects)
    ///    Counting number of foreign entities. Dynamically created by <see cref="PropertyKeyMapper.MapEnum"/>
    ///    
    /// NOTE: <see cref="AggregationKey"/> is not to be confused with <see cref="ForeignKeyAggregateKey"/>
    /// </summary>
    public class ForeignKeyAggregateKey : PropertyKey {

        public AggregationType AggregationType { get; private set; }
        /// <summary>
        /// Must be specified since <see cref="ForeignProperty"/> may have multiple values in <see cref="PropertyKeyAttribute.Parents"/>
        /// </summary>
        public Type ForeignEntity { get; private set; }
        public PropertyKey ForeignProperty { get; private set; }

        ///// <summary>
        ///// Private in order to limit number of possible objects created (by going through cache with <see cref="Get"/>. 
        ///// </summary>
        ///// <param name="key"></param>

        /// <summary>
        /// Should only be called at application startup through <see cref="PropertyKeyMapper"/>
        /// </summary>
        /// <param name="aggregationType"></param>
        /// <param name="foreignEntity"></param>
        /// <param name="foreignProperty"></param>
        /// <param name="key"></param>
        public ForeignKeyAggregateKey(AggregationType aggregationType, Type foreignEntity, PropertyKey foreignProperty, PropertyKeyAttributeEnriched key) : base(key) {
            AggregationType = aggregationType;
            ForeignEntity = foreignEntity;
            ForeignProperty = foreignProperty;
        }

        //private static ConcurrentDictionary<string, AggregationKey> _aggregations = new ConcurrentDictionary<string, AggregationKey>();
        //public static AggregationKey Get(AggregationType aggregationType, PropertyKey foreignKey) => _aggregations.GetOrAdd(
        //    aggregationType + "_" + foreignKey.Key.PToString, key => {

        //        var retval = new AggregationKey( /// Note that ideally this should only happen at application startup (<see cref="PropertyKeyMapper.MapEnum{T}"/> / <see cref="Startup.Initialize{TPerson}"/>)
        //            aggregationType,
        //            foreignKey,
        //            new PropertyKeyAttributeEnrichedDyn(
        //                new PropertyKeyAttribute(
        //                    property: key,
        //                    description: foreignKey.Key.A.Parents.Single(() => k "-" + aggregationType + "- for -" + foreignKey.Key.PToString + "-",
        //                    longDescription: "",
        //                    isMany: false
        //                ) {
        //                    Type = typeof(long) /// TODO: Maybe allow double also for aggregations?
        //                },
        //                (CoreP)PropertyKeyMapper.GetNextCorePId()
        //            )
        //        );
        //        PropertyKeyMapper.AddA(retval); // Add because we want to recognize these when later read from database
        //        return retval;
        //    });
    }
}
