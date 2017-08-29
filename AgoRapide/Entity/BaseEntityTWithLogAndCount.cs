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

        public void Count(Type type, CountP id) => Count(AggregationKey.GetAggregationKey(AggregationType.Count, type, id.A()));
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
    /// Two examples of aggregation:
    /// 1) <see cref="AggregationType.Sum"/>, <see cref="Person"/>, Salary. 
    ///    Used for instance for a given <see cref="Context"/>, giving statistics about the data seen.
    /// 2) <see cref="AggregationType.Count"/>, <see cref="Person"/>, <see cref="CountP.Created"/>
    ///    Typically used for communicating result of API call to client. 
    /// </summary>
    public class AggregationKey : PropertyKey {
        public AggregationKey(PropertyKeyAttributeEnriched key) : base(key) { }

        private static ConcurrentDictionary<string, AggregationKey> _aggregations = new ConcurrentDictionary<string, AggregationKey>();

        public static AggregationKey GetAggregationKey(AggregationType aggregationType, Type entityType, PropertyKey property) => _aggregations.GetOrAdd(
            aggregationType + "_" + entityType.ToStringVeryShort() + "_" + property.Key.PToString, key => {
                var retval = new AggregationKey( /// Note that ideally this should only happen at application startup (<see cref="PropertyKeyMapper.MapEnum{T}"/> / <see cref="Startup.Initialize{TPerson}"/>)
                    new PropertyKeyAttributeEnrichedDyn(
                        new PropertyKeyAttribute(
                            property: key,
                            description: "-" + aggregationType + "- for -" + entityType.ToStringVeryShort() + "- -" + property.Key.PToString + "-",
                            longDescription: "",
                            isMany: false
                        ) {
                            Type = typeof(long) /// TODO: Maybe allow double also for aggregations?
                        },
                        (CoreP)PropertyKeyMapper.GetNextCorePId()
                    )
                );
                PropertyKeyMapper.AddA(retval);
                return retval;
            });
    }

    /// <summary>
    /// TODO: Move to separate file
    /// </summary>
    public enum AggregationType {
        None,
        Count,
        Sum,
        Min,
        Max
    }
}
