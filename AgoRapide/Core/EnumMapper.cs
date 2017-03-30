using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {

    /// <summary>
    /// Helper class matching entity property enums (like P) used in your project to <see cref="CoreProperty"/>. 
    /// Note especially <see cref="GetCPA(string, IDatabase)"/> which is able to store in database any new string values found.
    /// </summary>
    public static class EnumMapper {

        /// <summary>
        /// Key is enum type which is mapped from. Value is dictionary with values for each enum-value again. 
        /// Note how adding to this dictionary is supposed to be always done by a single thread through <see cref="MapEnum{T}"/>. 
        /// </summary>
        private static Dictionary<Type, Dictionary<int, AgoRapideAttributeEnriched>> fromEnumMaps = new Dictionary<Type, Dictionary<int, AgoRapideAttributeEnriched>>();

        /// <summary>
        /// TODO: Define atomic increasing of this value.
        /// </summary>
        private static int lastCorePropertyId = (int)(object)Util.EnumGetValues<CoreProperty>().Max();
        private static int GetNextCorePropertyId() => System.Threading.Interlocked.Increment(ref lastCorePropertyId);

        /// <summary>
        /// Key is string which is mapped from. 
        /// Populated through <see cref="MapEnum{T}"/>. 
        /// Used at ordinary reading from database. 
        /// </summary>
        private static ConcurrentDictionary<string, AgoRapideAttributeEnriched> fromStringMaps = new ConcurrentDictionary<string, AgoRapideAttributeEnriched>();

        /// <summary>
        /// The order in which <see cref="MapEnum"/> is being called. 
        /// </summary>
        private static List<Type> mapOrders = new List<Type>();

        //public static void AssertNoPreviousInitializationOfAttributeIfEntityPropertyEnum<T>(T _enum) {
        //    if (!mapOrders.Contains(typeof(T))) return; 
        //    if (from)
        //}

        // private static HashSet<Type> mappedEnums = new HashSet<Type>();
        private static Dictionary<Type, List<string>> overriddenAttributes = new Dictionary<Type, List<string>>();
        /// <summary>
        /// TODO: Rename into something else. MapEnum for instance.
        /// 
        /// Register typeof(<typeparamref name="T"/>) for later use by <see cref="GetCPA{T}"/>
        /// 
        /// Not thread-safe. Only to be used by single thread at application initialization. 
        /// 
        /// TODO: EXPAND ON THIS EXPLANATION:
        /// Note how name collisions against <see cref="fromStringMaps"/> are simply ignored. 
        /// Client is expected to call <see cref="MapEnum{T}"/> starting with the innermost library, like <see cref="MapEnum"/> for <see cref="CoreProperty"/>
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
                "This is quite normal if for instance multiple assemblies calls " + nameof(MapEnum) + " for " + nameof(CoreProperty));
                return;
            }
            _allCoreProperty = null; // TODO: REMOVE USE OF THIS!
            overriddenAttributes[typeof(T)] = new List<string>();
            // fromEnumMaps.Add(typeof(T), Util.EnumGetValues<T>().ToDictionary(e => (int)(object)e, e => {
            Util.EnumGetValues<T>().ToDictionary(e => (int)(object)e, e => {
                var coreProperty = e is CoreProperty ? (CoreProperty)(object)e : (CoreProperty)GetNextCorePropertyId();
                var retval = e.GetAgoRapideAttributeT(coreProperty);
                if (fromStringMaps.TryGetValue(e.ToString(), out var existing)) {
                    overriddenAttributes.GetValue(existing.A.Property.GetType(), () => nameof(T) + ": " + typeof(T)).Add(
                        existing.A.Property.GetType().ToStringShort() + "." + existing.A.Property + " replaced by " + typeof(T).ToStringShort() + "." + e);
                }
                fromStringMaps[e.ToString()] = retval; // TODO: Consider handling name collisions differently here... 
                return retval;
            });
            mapOrders.Add(typeof(T));
        }

        /// <summary>
        /// To be called once at application initialization.
        /// 
        /// Note how subsequent calls to <see cref="Extensions.GetAgoRapideAttributeT"/> will then always return the overridden values if any.
        /// </summary>
        public static void MapEnumFinalize(Action<string> noticeLogger) =>
            mapOrders.ForEach(o => {
                var overridden = overriddenAttributes.GetValue(o);
                noticeLogger(
                    "Replacements for " + o.ToStringShort() + ":\r\n" +
                    (overridden.Count == 0 ? "[NONE]" : string.Join("\r\n", overriddenAttributes.GetValue(o)))
                );
                Extensions.ReplaceAgoRapideAttribute(o, fromEnumMaps.GetValue(o).ToDictionary(e => e.Key, e =>
                /// TODO: We must also replace for "manually" given <see cref="CoreProperty"/>
                    fromStringMaps.GetValue(e.Value.A.Property.ToString(), () => nameof(o) + ": " + o)
                ));
            });

        /// <summary>
        /// Note how this is reset whenever <see cref="fromEnumMaps"/> is added to
        /// </summary>
        private static List<AgoRapideAttributeEnriched> _allCoreProperty;
        /// <summary>
        /// Returns all <see cref="CoreProperty"/> including additional ones mapped from other enums. 
        /// TODO: TRY TO REMOVE USE OF THIS!
        /// </summary>
        public static List<AgoRapideAttributeEnriched> AllCoreProperty => _allCoreProperty ?? (_allCoreProperty = fromEnumMaps.TryGetValue(typeof(CoreProperty), out var retval) ? retval.Values.ToList() : throw new InvalidMappingException("Most probably because no corresponding call was made to " + nameof(MapEnum)));
        /// <summary>
        /// Preferred method when <paramref name="_enum"/> is known in the C# code
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_enum"></param>
        /// <returns></returns>
        public static AgoRapideAttributeEnriched GetCPA<T>(T _enum) where T : struct, IFormattable, IConvertible, IComparable =>  // What we really would want is "where T : Enum"
            TryGetCPA(_enum, out var retval) ? retval : throw new InvalidMappingException<T>(_enum, "Most probably because " + _enum + " is not a valid member of " + typeof(T));

        /// <summary>
        /// TODO: REMOVE THIS METHOD!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_enum"></param>
        /// <returns></returns>
        public static AgoRapideAttributeEnriched GetCPAOrDefault<T>(T _enum) where T : struct, IFormattable, IConvertible, IComparable =>  // What we really would want is "where T : Enum"
            TryGetCPA(_enum, out var retval) ? retval : throw new NullReferenceException("Unable to return default, concept does not exist");

        /// <summary>
        /// Note how <see cref="InvalidMappingException{T}"/> is being thrown if no corresponding call was made to <see cref="MapEnum"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_enum"></param>
        /// <param name="cpa"></param>
        /// <returns></returns>
        public static bool TryGetCPA<T>(T _enum, out AgoRapideAttributeEnriched cpa) where T : struct, IFormattable, IConvertible, IComparable =>  // What we really would want is "where T : Enum"
            fromEnumMaps.TryGetValue(typeof(T), out var dict) ?
                dict.TryGetValue((int)(object)_enum, out cpa) :
                throw new InvalidMappingException<T>(_enum,
                    "Most probably because no corresponding call was made to " + nameof(MapEnum) + " for " + typeof(T) + ".\r\n" +
                    "(Hint: this is usually done in Startup.cs.)");

        /// <summary>
        /// TODO: Find a better name! Try to avoid use of this method. 
        /// Necessary to use when <paramref name="_enum"/> originates from <see cref="AgoRapideAttribute.Property"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_enum"></param>
        /// <param name="cpa"></param>
        /// <returns></returns>
        public static bool TryGetCPAAsObject(object _enum, out AgoRapideAttributeEnriched cpa) {
            if (!(_enum?.GetType() ?? throw new NullReferenceException(nameof(_enum))).IsEnum) throw new InvalidObjectTypeException(_enum, "Expected " + nameof(Type.IsEnum));
            return fromEnumMaps.TryGetValue(_enum.GetType(), out var dict) ?
                dict.TryGetValue((int)_enum, out cpa) :
                throw new InvalidMappingException(
                    "Unable to map from " + _enum.GetType() + "." + _enum + ".\r\n" +
                    "Most probably because no corresponding call was made to " + nameof(MapEnum) + " for " + _enum.GetType() + ".\r\n" +
                    "(Hint: this is usually done in Startup.cs)");
        }
        /// <summary>
        /// TODO: REMOVE THIS METHOD!
        /// </summary>
        /// <param name="_enum"></param>
        /// <returns></returns>
        public static AgoRapideAttributeEnriched GetCPAOrDefault(string _enum) => fromStringMaps.TryGetValue(_enum, out var retval) ? retval : throw new NullReferenceException("Unable to return default, concept does not exist");
        public static AgoRapideAttributeEnriched GetCPA(string _enum) => fromStringMaps.GetValue(_enum);
        /// <summary>
        /// Method that will always "succeed" in the sense that unknown values of <paramref name="_enum"/> will just be added. 
        /// 
        /// Preferred method when <paramref name="_enum"/> is not known in the C# code. 
        /// 
        /// Also to be used for hierarchically organise enums.
        /// 
        /// This is the method to use when reading from database (before initializing <see cref="Property"/>) 
        /// and also when dynamically adding properties. 
        /// 
        /// Note how unknown values are added automatically and also stored in database
        /// </summary>
        /// <param name="_enum"></param>
        /// <returns></returns>
        public static AgoRapideAttributeEnriched GetCPA(string _enum, IDatabase db) =>
            fromStringMaps.GetOrAdd(_enum, e => {
                if (db == null) throw new NullReferenceException(nameof(db));
                throw new NotImplementedException(
                    "Set _allCoreProperty = null;" +
                    "TODO: Reuse " + nameof(EnumClass) + ". " +
                    "Store in database (check that not already exist, some reading from database must be done in Startup.cs). " +
                    "Corresponding AgoRapideAtribute properties to be stored in database. " +
                    "Also implement support for hierarchically organised enums where AgoRapideAttribute reflects all hierarchical levels");
            });

        public static bool TryGetCPA(string _enum, out AgoRapideAttributeEnriched cpa) => fromStringMaps.TryGetValue(_enum, out cpa);
    }

    ///// <summary>
    ///// Simple container class for <see cref="CoreProperty"/> and <see cref="AgoRapideAttributeT"/>
    ///// TODO: Make immutable
    ///// 
    ///// TODO: Consider just removing altogether.
    ///// </summary>
    //public class CPA {
    //    public CoreProperty cp;
    //    public AgoRapideAttributeEnriched a;

    //    public static CPA Default = new CPA { cp = CoreProperty.None, a = CoreProperty.None.GetAgoRapideAttributeT() };
    //}
}