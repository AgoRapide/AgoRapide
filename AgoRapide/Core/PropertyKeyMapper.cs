// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;
using AgoRapide;

namespace AgoRapide.Core {

    [Class(
        Description = "Helper class matching -" + nameof(EnumType.PropertyKey) + "- (like P) used in your project to -" + nameof(CoreP) + "-",
        LongDescription = "Note especially -" + nameof(GetA) + "- which is able to store in database any new string values found"
    )]
    public static class PropertyKeyMapper {

        /// <summary>
        /// Set by <see cref="MapEnumFinalize"/>
        /// </summary>
        private static List<PropertyKey> _allCoreP;
        /// <summary>
        /// Returns all <see cref="CoreP"/> including additional ones mapped from other enums. 
        /// 
        /// Will not contain <see cref="PropertyKeyAttributeEnrichedDyn"/> (since all use of <see cref="AllCoreP"/> is based on C# originated needs, not database originated needs)
        /// </summary>
        public static List<PropertyKey> AllCoreP => _allCoreP ?? throw new NullReferenceException(nameof(AllCoreP) + ". Most probably because no corresponding call was made to " + nameof(MapEnumFinalize));

        /// <summary>
        /// Key is enum type which is mapped from. 
        /// Value is dictionary with values for each enum-value again. 
        /// Note how adding to this dictionary is supposed to be always done by a single thread through <see cref="MapEnum{T}"/>. 
        /// TODO: NOT TRUE as of Apr 2016 as <see cref="TryAddA"/> also has to add properties to this collection.
        /// 
        /// Is in principle equivalent to <see cref="Extensions._agoRapideAttributeTCache"/> except that _that_ cache also contains 
        /// entries for non <see cref="AgoRapide.EnumType.PropertyKey"/> 
        /// (while _this_ cache, <see cref="_cache"/> only contains entires for entity property enums)
        /// </summary>
        private static Dictionary<Type, Dictionary<int, PropertyKey>> _cache = new Dictionary<Type, Dictionary<int, PropertyKey>>();

        private static int lastCorePId = (int)(object)Util.EnumGetValues<CoreP>().Max();
        public static int GetNextCorePId() => System.Threading.Interlocked.Increment(ref lastCorePId);

        /// <summary>
        /// Key is string which is mapped from. 
        /// Populated through <see cref="MapEnum{T}"/>. 
        /// Used at ordinary reading from database. 
        /// </summary>
        private static ConcurrentDictionary<string, PropertyKey> _fromStringMaps = new ConcurrentDictionary<string, PropertyKey>();

        /// <summary>
        /// The order in which <see cref="MapEnum"/> was being called.
        /// Only used temporarily by <see cref="MapEnum{T}"/> and <see cref="MapEnumFinalize"/> (when called from <see cref="Startup.Initialize{TPerson}"/>). 
        /// </summary>
        private static List<Type> mapOrders = new List<Type>();

        /// <summary>
        /// Overrides for each type. 
        /// Only used temporarily at application startup by <see cref="MapEnum{T}"/> and <see cref="MapEnumFinalize"/>
        /// </summary>
        private static Dictionary<Type, List<string>> overriddenAttributes = new Dictionary<Type, List<string>>();

        public static void MapKnownEnums(Action<string> noticeLogger) {
            MapEnum<DBField>(noticeLogger); /// This is a quasi <see cref="AgoRapide.Core.PropertyKeyAttribute"/>
            MapEnum<CoreP>(noticeLogger);
            MapEnum<ConfigurationAttribute.ConfigurationP>(noticeLogger);
            MapEnum<CountP>(noticeLogger);
            MapEnum<API.APIMethodP>(noticeLogger);
            MapEnum<API.ResultP>(noticeLogger);
            MapEnum<SynchronizerP>(noticeLogger);
            MapEnum<PersonP>(noticeLogger);
        }
        /// <summary>
        /// TODO: Rename into something else. MapEnum for instance.
        /// 
        /// Register typeof(<typeparamref name="T"/>) for later use by <see cref="GetA{T}"/>
        /// 
        /// Not thread-safe. Only to be used by single thread at application initialization. 
        /// 
        /// TODO: EXPAND ON THIS EXPLANATION:
        /// Note how name collisions against <see cref="_fromStringMaps"/> are simply ignored. 
        /// Client is expected to call <see cref="MapEnum{T}"/> starting with the innermost library, like <see cref="MapEnum"/> for <see cref="CoreP"/>
        /// and then moving outwards towards the final application layer. 
        /// This results in the final application being able to override settings done by the core library.
        /// End with a call to <see cref="MapEnumFinalize"/>. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="noticeLogger">
        /// Used for logging notices about mapping process.
        /// TODO: What about simply returning a string for logging instead?
        /// </param>
        public static void MapEnum<T>(Action<string> noticeLogger) where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (overriddenAttributes.ContainsKey(typeof(T))) {
                noticeLogger( // TODO: Consider eliminating this possibility through smarter implementation and instead throwing exception now
                "NOTICE: Duplicate calls made to " + nameof(MapEnum) + " for " + typeof(T) + ". " +
                "This is quite normal if for instance multiple assemblies calls " + nameof(MapEnum) + " for " + nameof(CoreP));
                return;
            }
            _allCoreP = null; // TODO: REMOVE USE OF THIS!
            overriddenAttributes[typeof(T)] = new List<string>();
            var excludedValue = typeof(T).Equals(typeof(DBField)) ? (T)(object)-1 : (T)(object)0;
            if (!"None".Equals(excludedValue.ToString())) throw new InvalidEnumException(excludedValue,
                "The string representation corresponding to " + ((int)(object)excludedValue).ToString() + " for enum " + typeof(T) + " is expected to be 'None', not " + excludedValue.ToString() + ".\r\n" +
                "(The rationale for this is to be able to catch values not being set propertly.)\r\n" +
                "Possible resolution: Add 'None' as first element of enum " + typeof(T));

            Util.EnumGetValues(excludedValue).ForEach(e => { /// Note exception for <see cref="DBField"/>. TODO: Try to remove this! 
                // TODO: WHY DOES THIS WORK FOR IsMany???
                var a = new PropertyKey(new PropertyKeyAttributeEnrichedT<T>(PropertyKeyAttribute.GetAttribute(e), e is CoreP ? (CoreP)(object)e : (CoreP)GetNextCorePId()));
                a.SetPropertyKeyWithIndexAndPropertyKeyAsIsManyParentOrTemplate(); // HACK
                if (_fromStringMaps.TryGetValue(e.ToString(), out var existing)) {
                    overriddenAttributes.GetValue(existing.Key.A.EnumValue.GetType(), () => nameof(T) + ": " + typeof(T)).Add(
                        existing.Key.A.EnumValue.GetType().ToStringShort() + "." + existing.Key.A.EnumValue + " replaced by " + typeof(T).ToStringShort() + "." + e);
                }
                _fromStringMaps[e.ToString()] = a;
                if (a.Key.A.InheritFrom != null && !a.Key.A.InheritFrom.ToString().Equals(e.ToString())) {
                    if (_fromStringMaps.TryGetValue(a.Key.A.InheritFrom.ToString(), out existing)) {
                        overriddenAttributes.GetValue(existing.Key.A.EnumValue.GetType(), () => nameof(T) + ": " + typeof(T)).Add(
                            existing.Key.A.EnumValue.GetType().ToStringShort() + "." + existing.Key.A.EnumValue + " replaced by " + typeof(T).ToStringShort() + "." + e);
                    }
                    _fromStringMaps[a.Key.A.InheritFrom.ToString()] = a;
                }

                if (a.Key.A.Parents != null) { /// Create all actual <see cref="AggregationKey"/> now (reduces chances of conflict later in <see cref="AddA"/>)
                    a.Key.A.AggregationTypes.ToList().ForEach(aggregationType => {
                        a.Key.A.Parents.ToList().ForEach(parent => {
                            var dummy = AggregationKey.GetAggregationKey(aggregationType, parent, a); /// This will call back again to <see cref="AddA"/>, storing in <see cref="_fromStringMaps"/>
                        });
                    });
                }
                var test = a.Key.A.EnumValueExplained;
            });
            mapOrders.Add(typeof(T));
        }

        /// <summary>
        /// To be called once at application initialization.
        /// </summary>
        public static void MapEnumFinalize(Action<string> noticeLogger) {
            mapOrders.ForEach(o => {
                var overridden = overriddenAttributes.GetValue(o);
                noticeLogger(
                    "\r\n\r\nReplacements for " + o.ToStringShort() + ":\r\n" +
                    (overridden.Count == 0 ? "[NONE]\r\n" : string.Join("\r\n", overriddenAttributes.GetValue(o)) + "\r\n")
                );
                var dict = Util.EnumGetValues(o).ToDictionary(e => (int)e, e =>
                    /// TODO: We must also replace for "manually" given <see cref="CoreP"/>
                    _fromStringMaps.GetValue(e.ToString(), () => nameof(o) + ": " + o)
                );
                _cache[o] = dict;
            });
            var enumMapForCoreP = _cache.GetValue(typeof(CoreP), () => typeof(CoreP) + " expected to be in " + nameof(mapOrders) + " (" + string.Join(", ", mapOrders.Select(o => o.ToStringShort())) + ")");
            var allCoreP = new Dictionary<CoreP, PropertyKey>();
            _fromStringMaps.ForEach(e => {
                if (!allCoreP.TryGetValue(e.Value.Key.CoreP, out var existing)) {
                    allCoreP.AddValue(e.Value.Key.CoreP, e.Value);
                } else {
                    /// Keep the one that is last in <see cref="mapOrders"/>
                    if (mapOrders.IndexOf(e.Value.Key.A.EnumValue.GetType()) > mapOrders.IndexOf(existing.Key.A.EnumValue.GetType())) {
                        /// The new one came later as parameter to <see cref="MapEnum{T}"/> and should take precedence
                        allCoreP[e.Value.Key.CoreP] = e.Value;
                    }
                }
                if (!enumMapForCoreP.ContainsKey((int)e.Value.Key.CoreP)) enumMapForCoreP.Add((int)e.Value.Key.CoreP, e.Value); /// This ensures that <see cref="TryGetA{T}(T, out PropertyKeyAttributeEnriched)"/> also works as intended (accepting "int" as parameter as long as it is mapped)
            });
            _allCoreP = allCoreP.Values.ToList();
        }

        public static void MapEnumAssert(Action<string> noticeLogger) {

        }

            /// <summary>
            /// Preferred method when <paramref name="_enum"/> is known in the C# code
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="_enum"></param>
            /// <returns></returns>
            public static PropertyKey GetA<T>(T _enum) where T : struct, IFormattable, IConvertible, IComparable =>  // What we really would want is "where T : Enum"
            TryGetA(_enum, out var retval) ? retval : throw new InvalidMappingException<T>(_enum, "Most probably because " + _enum + " is not a valid member of " + typeof(T));

        /// <summary>
        /// Note how <see cref="InvalidMappingException{T}"/> is being thrown if no corresponding call was made to <see cref="MapEnum"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_enum"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool TryGetA<T>(T _enum, out PropertyKey key) where T : struct, IFormattable, IConvertible, IComparable =>  // What we really would want is "where T : Enum"
            _cache.TryGetValue(typeof(T), out var dict) ?
                dict.TryGetValue((int)(object)_enum, out key) :
                throw new InvalidMappingException<T>(_enum, GetProbableCauseAndResolutionForInvalidMapping(typeof(T)));

        public static PropertyKey GetA(string _enum) => _fromStringMaps.GetValue(_enum);

        /// <summary>
        /// Called from <see cref="PostgreSQLDatabase"/>.ReadOneProperty.
        /// </summary>
        /// <param name="_enum"></param>
        /// <param name="isMany"></param>
        /// <param name="description"></param>
        /// <param name="strErrorResponse"></param>
        /// <returns></returns>
        public static bool TryAddA(string _enum, bool isMany, string description, out string strErrorResponse) {
            if (!InvalidIdentifierException.CSharpCodeDomProvider.IsValidIdentifier(_enum)) {
                /// NOTE: Note how this check is extremely important in order to protect against 
                /// NOTE: both SQL injection attacks and scripting attacks on HTML web pages. 
                /// NOTE: This is so because it is envisaged that the final user shall be able to 
                /// NOTE: create new "enums" / <see cref="PropertyKeyWithIndex"/> in the system. 
                /// NOTE: And since the corresponding <see cref="PropertyKeyAttributeEnriched.PToString"/> 
                /// NOTE: now being created will be "trusted" throughout the system, it must be asserted safe 
                /// TODO: here at the origin.
                strErrorResponse = nameof(_enum) + " (" + _enum + ") is not a valid C# identifier";
                return false;
            }

            // TODO: This should not have been accepted for IsMany!
            var key = new PropertyKey( // Note that 
                new PropertyKeyAttributeEnrichedDyn(
                    new PropertyKeyAttribute(
                        property: _enum,
                        description: description,
                        longDescription: "This is a dynamically added 'enum' created by " + nameof(PropertyKeyMapper) + "." + System.Reflection.MethodBase.GetCurrentMethod().Name,
                        isMany: isMany
                    ),
                    (CoreP)GetNextCorePId()
                )
            );
            if (!_enum.Equals(key.Key.PToString)) throw new PropertyKeyWithIndex.InvalidPropertyKeyException(nameof(_enum) + " (" + _enum + ") != " + nameof(key.Key.PToString) + " (" + key.Key.PToString + ")");

            AddA(key);

            strErrorResponse = null;
            return true;
        }

        /// <summary>
        /// TODO: Correct not thread safe use of <see cref="_cache"/> in <see cref="PropertyKeyMapper.AddA"/>
        /// </summary>
        /// <param name="key"></param>
        [ClassMember(Description = "Adds -" + nameof(AggregationKey) + "- or attribute originating dynamically")]
        public static void AddA(PropertyKey key) {
            key.SetPropertyKeyWithIndexAndPropertyKeyAsIsManyParentOrTemplate();

            // _allCoreP.Add(key); Not needed (and not thread-safe either)
            if (!_fromStringMaps.TryAdd(key.Key.PToString, key)) {
                switch (key) {
                    case AggregationKey aggregationKey:
                        /// We are been called from <see cref="AggregationKey.GetAggregationKey"/>, but key already exists. 
                        /// This would typically happen if all possible combinations of aggregations are not registered at application startup
                        /// resulting in <see cref="TryAddA"/> having already been called when unknown string was found in database, and then later (that is now), 
                        /// we get a name collision when <see cref="AggregationKey.GetAggregationKey"/> is being called. 
                        throw new PropertyKeyWithIndex.InvalidPropertyKeyException(
                            nameof(key.Key.PToString) + " already exists for " + key.Key.ToString() + ".\r\n" +
                            "Possible cause: Aggregation combination was not registered at application startup and there are properties already stored in database.");
                    default:
                        // This could just be a thread issue. In other words we could choose to just ignore this exception. 
                        // or we could instead just do 
                        //   _fromStringMaps[key.Key.PToString] = key;
                        throw new PropertyKeyWithIndex.InvalidPropertyKeyException(nameof(key.Key.PToString) + " already exists for " + key.Key.ToString());
                }
            }

            /// TODO: NOT THREAD SAFE
            /// TODO: Correct this not thread safe use of <see cref="_cache"/> in <see cref="TryAddA"/>
            /// TODO: NOT THREAD SAFE
            _cache.GetValue(typeof(CoreP))[(int)key.Key.CoreP] = key;

            // TODO: STORE THIS IN DATABASE
            // TODO: THINK ABOUT THREAD ISSUES 
            // TODO: READ AT STARTUP!!! (Take into consideration later adding to C# code of _enum)
        }

        public static bool TryGetA(string _enum, out PropertyKey key) => _fromStringMaps.TryGetValue(_enum, out key);

        public static string GetProbableCauseAndResolutionForInvalidMapping(Type type) =>
            "Most probably because no corresponding call was made to " + nameof(PropertyKeyMapper) + "." + nameof(MapEnum) + " for " + type + ".\r\n" +
            "Possible resolution:\r\n" +
            "Add the statement\r\n" +
            nameof(PropertyKeyMapper) + "." + nameof(MapEnum) + "<" + type + ">()\r\n" +
            "to Startup.cs (as of Jun 2017 look for 'mapper1<...>')."; /// See also <see cref="MapKnownEnums"/> of course
    }
}