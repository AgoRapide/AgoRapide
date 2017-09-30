// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;
using AgoRapide.Database;

/// <summary>
/// Note: <see cref="AgoRapide.AggregationKey"/> also resides in this file for the time being (see below). 
/// </summary>
namespace AgoRapide {

    /// <summary>
    /// TODO: This class has marginal value. 
    /// TODO: Since all data is stored in the ordinary <see cref="BaseEntity.Properties"/>-collection 
    /// TODO: we could just as well move all the methods here into <see cref="BaseEntity"/> and having this functionality available for all classes.
    /// </summary>
    [Class(Description =
        "Extension on -" + nameof(BaseEntity) + "- with internal logging and counting of vital statistics.\r\n" +
        "Useful for:\r\n" +
        "1) Long-lived classes like -" + nameof(ApplicationPart) + "- where you want to record different kind of statistics for their use.\r\n" +
        "2) Classes for which it is desireable to communicate details about their contents like -" + nameof(Result) + "-.\r\n" +
        "\r\n" +
        "Examples of inheriting classes in AgoRapide are:\r\n" +
        "-" + nameof(ApplicationPart) + "-\r\n" +
        "-" + nameof(Result) + "-\r\n" +
        "-" + nameof(APIMethod) + "-\r\n"
    )]
    public abstract class BaseEntityWithLogAndCount : BaseEntity {

        public BaseEntityWithLogAndCount() => Properties = new Dictionary<CoreP, Property>(); // TODO: Initialize more deterministically for the different classes.
        
        [ClassMember(Description = "In-memory only logging (often used for short-lived logging like with -" + nameof(Result) + "-).")]
        public void LogInternal(string text, Type callerType, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") {
            text = DateTime.Now.ToString(DateTimeFormat.DateHourMinSecMs) + ": " + callerType.ToStringShort() + "." + caller + ": " + text + "\r\n";
            if (!Properties.TryGetValue(CoreP.Log, out var p)) {
                Properties.AddValue(CoreP.Log, new PropertyLogger(CoreP.Log.A().PropertyKeyWithIndex, initialValue: text));
            } else {
                (p as PropertyLogger ?? throw new InvalidObjectTypeException(p, typeof(PropertyLogger))).Log(text);
            }
        }

        public void Count(Type type, CountP id) => Count(AggregationKey.Get(AggregationType.Count, type, id.A()));
        public void Count(PropertyKey id) => Count(id.Key.CoreP, 1);
        public void Count(CoreP id, long increment) {
            if (!Properties.TryGetValue(id, out var p)) {
                Properties.AddValue(id, new PropertyCounter(id.A().PropertyKeyWithIndex, initialValue: increment));
            } else {
                (p as PropertyCounter ?? throw new InvalidObjectTypeException(p, typeof(PropertyCounter))).Count(increment);
            }
        }        
    }

    /// <summary>
    /// Note that you are not limited to using only <see cref="CountP"/>-values in you application. 
    /// <see cref="BaseEntityWithLogAndCount.Count"/> operates on any <see cref="EnumType.PropertyKey"/>. 
    /// 
    /// Note that although you can use these enum values directly, they are usually used within a <see cref="AggregationKey"/> in order to 
    /// specify entity type and <see cref="AggregationType"/>. 
    /// </summary>
    [Enum(AgoRapideEnumType = EnumType.PropertyKey)]
    public enum CountP {

        None,

        // TODO: Clean up in the use of values here.

        [PropertyKey(
            Description = "Entities created",
            AggregationTypes = new AggregationType[] { AggregationType.Count },
            Type = typeof(long))]
        Created,

        [PropertyKey(
            Description = "Entities still valid (usually in connection with -" + nameof(PropertyOperation.SetValid) + "-).",
            AggregationTypes = new AggregationType[] { AggregationType.Count },
            Type = typeof(long))]
        StillValid,

        [PropertyKey(
            Description = "Entities changed (usually in connection with -" + nameof(BaseDatabase.UpdateProperty) + "-).",
            AggregationTypes = new AggregationType[] { AggregationType.Count },
            Type = typeof(long))]
        Changed,

        [PropertyKey(
            Description = "Entities set invalid (usually in connection with -" + nameof(PropertyOperation.SetInvalid) + "-).",
            AggregationTypes = new AggregationType[] { AggregationType.Count },
            Type = typeof(long))]
        SetInvalid,

        [PropertyKey(
            Description = "Entities affected (usually in connection with -" + nameof(PropertyOperation) + "-).",
            AggregationTypes = new AggregationType[] { AggregationType.Count },
            Type = typeof(long))]
        Affected,

        [PropertyKey(
            Description = "Total entities considered.",
            AggregationTypes = new AggregationType[] { AggregationType.Count },
            Type = typeof(long))]
        Total,
    }

    public static class ExtensionsCountP {
        public static PropertyKey A(this CountP p) => PropertyKeyMapper.GetA(p);
    }

    /// <summary>
    /// TODO: Move to separate file
    /// 
    /// Some examples of aggregation:
    /// (work on this just started summer 2017)
    /// 
    /// 1) <see cref="AggregationType.Count"/>, <see cref="Person"/>, <see cref="CountP.Created"/>
    ///    Typically used for communicating result of API call to client. 
    ///    
    /// 2) <see cref="AggregationType.Sum"/>, <see cref="Person"/>, PersonP.Salary. 
    ///    Used for instance for a given <see cref="Context"/>, giving statistics about the data seen.
    ///    TODO: NOT IMPLEMENTED AS OF SEP 2017!
    /// 
    /// NOTE: <see cref="AggregationKey"/> is not to be confused with <see cref="PropertyKeyForeignKeyAggregate"/>
    /// </summary>
    public class AggregationKey : PropertyKey {

        public AggregationType AggregationType { get; private set; }
        public Type EntityType { get; private set; }

        /// <summary>
        /// Private in order to limit number of possible objects created (by going through cache with <see cref="Get"/>. 
        /// </summary>
        /// <param name="key"></param>
        private AggregationKey(AggregationType aggregationType, Type entityType, PropertyKeyAttributeEnriched key) : base(key) {
            AggregationType = aggregationType;
            EntityType = entityType;
        }

        private static ConcurrentDictionary<string, AggregationKey> _aggregations = new ConcurrentDictionary<string, AggregationKey>();
        public static AggregationKey Get(AggregationType aggregationType, Type entityType, PropertyKey property) => _aggregations.GetOrAdd(
            aggregationType + "_" + entityType.ToStringVeryShort() + "_" + property.Key.PToString, key => {

                // Added 14 Sep 2017. May have to relax a bit here though. 
                // Removed 27 Sep 2017. 
                // InvalidTypeException.AssertEquals(property.Key.A.Type, typeof(long), () => "Details: " + property.ToString());
                // TODO: Consider replacing with checking that supports + operator or similar.

                /// NOTE: Ideally, calls to this method should only happen at application startup (<see cref="PropertyKeyMapper.MapEnum{T}"/> / <see cref="Startup.Initialize{TPerson}"/>)
                /// NOTE: If restriction below is impractical we may remove this call to <see cref="Util.AssertCurrentlyStartingUp"/> since
                /// NOTE: code here is thread-safe anyway (but most probably your code is fully able to find all keys at startup anyway). 
                Util.AssertCurrentlyStartingUp(); // May be removed

                var retval = new AggregationKey( 
                    aggregationType,
                    entityType, 
                    new PropertyKeyAttributeEnrichedDyn(
                        new PropertyKeyAttribute(
                            property: key,
                            description: "-" + aggregationType + "- for -" + entityType.ToStringVeryShort() + "- -" + property.Key.PToString + "-",
                            longDescription: "",
                            isMany: false
                        ) {
                            /// Note how <see cref="PropertyKeyAttribute.Parents"/> is not relevant here, as there is no defined place for which
                            /// this <see cref="AggregationKey"/> is to be stored. Most probably it just exists temporarily within a
                            /// <see cref="BaseEntityWithLogAndCount"/>-instance. 
                            
                            Type = typeof(long) /// TODO: Maybe allow double also for aggregations?
                        },
                        (CoreP)PropertyKeyMapper.GetNextCorePId()
                    )
                );
                PropertyKeyMapper.AddA(retval); // Add because we want to recognize these when later read from database
                return retval;
            });
    }

    /// <summary>
    /// TODO: Move to separate file
    /// </summary>
    [Enum(AgoRapideEnumType = EnumType.EnumValue)]
    public enum AggregationType {
        None,
        Count,
        Sum,
        Min,
        Max
    }
}