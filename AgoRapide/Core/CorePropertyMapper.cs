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
    /// Helper class matching <see cref="CoreProperty"/> to the chosen <see cref="TProperty"/> used in your project (usually P).
    /// 
    /// Usually initialized like
    ///    static CorePropertyMapper[P] _cpm = new CorePropertyMapper[P]();
    /// together with 
    ///    static TProperty M(CoreProperty coreProperty) => _cpm.Map(coreProperty);
    /// in all classes where you need this functionality.
    /// 
    /// TODO: Initialization of <see cref="dict"/> could accept missing CoreProperties by just mapping them to integer values not 
    /// TODO: defined as TProperty.
    /// </summary>
    public class CorePropertyMapper { 

        // ------- START OF New system (post v1.0) for reverse mapping, TOWARDS CoreProperty instead of FROM ------------------

        /// <summary>
        /// Key is enum type which is mapped from. Value is dictionary with values for each enum-value again. 
        /// Note how adding to this dictionary is supposed to be always done by a single thread through <see cref="RegisterEnum{T}"/>. 
        /// </summary>
        private static Dictionary<Type, Dictionary<int, CPA>> fromEnumMaps = new Dictionary<Type, Dictionary<int, CPA>>();

        /// <summary>
        /// TODO: Define atomic increasing of this value.
        /// </summary>
        private static int lastCorePropertyId = (int)(object)Util.EnumGetValues<CoreProperty>().Max();
        private static int GetNextCorePropertyId() => System.Threading.Interlocked.Increment(ref lastCorePropertyId);

        /// <summary>
        /// Key is string which is mapped from. 
        /// Populated through <see cref="RegisterEnum{T}"/>. 
        /// Used at ordinary reading from database. 
        /// </summary>
        private static ConcurrentDictionary<string, CPA> fromStringMaps = new ConcurrentDictionary<string, CPA>();

        /// <summary>
        /// Register typeof(<typeparamref name="T"/>) for later use by <see cref="Map2{T}"/>
        /// 
        /// Not thread-safe. Only to be used by single thread at application initialization. 
        /// 
        /// TODO: EXPAND ON THIS EXPLANATION:
        /// Note how name collisions against <see cref="fromStringMaps"/> are simply ignored. 
        /// Client is expected to call <see cref="RegisterEnum{T}"/> starting with the innermost library, like RegisterEnum{CoreProperty} 
        /// and then moving outwards towards the final application layer. 
        /// This results naturally in the final application being able to override settings done by the core library.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="noticeLogger">Used for logging notices about mapping process.</param>
        public static void RegisterEnum<T>(Action<string> noticeLogger) where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (fromEnumMaps.ContainsKey(typeof(T))) {
                noticeLogger( // TODO: Consider eliminating this possibility through smarter implementation and instead throwing exception now
                "NOTICE: Duplicate calls made to " + nameof(RegisterEnum) + " for " + typeof(T) + ". " +
                "This is quite normal if for instance multiple assemblies calls " + nameof(RegisterEnum) + " for " + nameof(CoreProperty));
                return;
            }
            fromEnumMaps.Add(typeof(T), Util.EnumGetValues<T>().ToDictionary(e => (int)(object)e, e => {
                var a = e.GetAgoRapideAttribute();
                var retval = (e is CoreProperty) ?
                    new CPA { cp = (CoreProperty)(object)e, a = a } :
                    new CPA { cp = (CoreProperty)GetNextCorePropertyId(), a = a };
                if (fromStringMaps.TryGetValue(e.ToString(), out var existing)) noticeLogger(
                    "NOTICE: Duplicate '" + e.ToString() + "'.\r\n" +
                    nameof(existing) + ": " + existing.cp.ToString() + "\r\n" + /// TODO: Change to <see cref="AgoRapideAttributeT.PToString"/>
                    "will be replaced by\r\n" +
                    nameof(retval) + ": " + retval.cp.ToString() + "\r\n" + /// TODO: Change to <see cref="AgoRapideAttributeT.PToString"/>
                    "This is quite normal as long as calls to " + nameof(RegisterEnum) + " " +
                    "are made in order of increasing distance from core AgoRapide-library " +
                    "giving final application code override authority over library definitions");
                fromStringMaps[e.ToString()] = retval; // TODO: Consider handling name collisions differently here... 
                return retval;
            }));
        }

        /// <summary>
        /// Preferred method when <paramref name="_enum"/> is known in the C# code
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_enum"></param>
        /// <returns></returns>
        public static CPA Map2<T>(T _enum) where T : struct, IFormattable, IConvertible, IComparable =>  // What we really would want is "where T : Enum"
            fromEnumMaps.TryGetValue(typeof(T), out var dict) ?
                (dict.TryGetValue((int)(object)_enum, out var retval) ? retval : throw new InvalidMappingException<T>(_enum, "Most probably because " + _enum + " is not a valid member of " + typeof(T))) :
                throw new InvalidMappingException<T>(_enum, "Most probably because no corresponding call was made to " + nameof(RegisterEnum));

        public static CPA Map2(string _enum) => Map2(_enum, null);
        /// <summary>
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
        public static CPA Map2(string _enum, IDatabase db) => 
            fromStringMaps.GetOrAdd(_enum, e => {
                throw new NotImplementedException(
                    "TODO: Reuse " + nameof(EnumClass) + ". " +
                    "Store in database (check that not already exist, some reading from database must be done in Startup.cs). " +
                    "Corresponding AgoRapideAtribute properties to be stored in database. " +
                    "Also implement support for hierarchically organised enums where AgoRapideAttribute reflects all hierarchical levels");
            });
        
        // ------- END OF New system (post v1.0) for reverse mapping, TOWARDS CoreProperty instead of FROM ------------------

        /// <summary>
        /// TODO: REMOVE THIS! 
        /// 
        /// Note how all valid <see cref="CoreProperty"/> values are asserted (at initialization of this class) that they match <see cref="TProperty"/> so
        /// any use of <see cref="Map"/>(<see cref="CoreProperty"/>) is guaranteed to give a result.
        /// </summary>
        public CoreProperty Map(CoreProperty coreProperty) => dict.GetValue2(coreProperty);

        /// <summary>
        /// TODO: REMOVE THIS! 
        /// 
        /// Note how "missing" <see cref="CoreProperty"/>-values are mapped silently to integer values "after" the max value for <see cref="TProperty"/>
        /// 
        /// Recursivity warning: Note how <see cref="Extensions.GetAgoRapideAttribute{T}"/> uses <see cref="CorePropertyMapper.dict"/> 
        /// which again uses <see cref="Extensions.GetAgoRapideAttribute{T}"/>. This recursive call is OK as long as 
        /// <see cref="CorePropertyMapper.dict"/> only calls <see cref="Extensions.GetAgoRapideAttribute{T}"/> with
        /// defined values for <see cref="TProperty"/>.
        /// </summary>
        private Dictionary<CoreProperty, CoreProperty> dict = new Func<Dictionary<CoreProperty, CoreProperty>>(() => {
            var retval = new Dictionary<CoreProperty, CoreProperty>();
            var lastId = (int)(object)Util.EnumGetValues<CoreProperty>().Max();
            Util.EnumGetValues<CoreProperty>().ForEach(coreProperty => {
                // See recursivity warning above.
                var matching = Util.EnumGetValues<CoreProperty>().Where(tProperty => tProperty.GetAgoRapideAttribute().A.CoreProperty == coreProperty).ToList();
                switch (matching.Count) {
                    case 0: retval[coreProperty] = (CoreProperty)(object)(++lastId); break; // Silently map those missing
                    case 1: retval[coreProperty] = matching[0]; break;
                    case 2: throw new CorePropertyNotMatchedException(coreProperty, "Multiple matches found (" + string.Join(", ", matching) + "), only one is allowed");
                }
            });
            return retval;
        })();

        private Dictionary<CoreProperty, CoreProperty> _reverseDict = null;
        private Dictionary<CoreProperty, CoreProperty> ReverseDict { // We can not use field initializer here because we have to refer to dict which is also field initialized
            get => _reverseDict ?? (_reverseDict = new Func<Dictionary<CoreProperty, CoreProperty>>(() => {
                var retval = new Dictionary<CoreProperty, CoreProperty>();
                dict.ForEach(e => retval.Add(e.Value, e.Key));
                return retval;
            })());
        }
        /// <summary>
        /// <see cref="MapReverse(TProperty)"/> is provided only for completeness. Most probably you need to use <see cref="TryMapReverse"/>
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public CoreProperty MapReverse(CoreProperty p) => TryMapReverse(p, out var retval) ? retval : throw new Exception("MapReverse failed for " + typeof(CoreProperty) + "." + p.ToString() + ", corresponding " + typeof(CoreProperty) + " not found");
        public bool TryMapReverse(CoreProperty p, out CoreProperty coreProperty) => ReverseDict.TryGetValue(p, out coreProperty);

        public class CorePropertyNotMatchedException : ApplicationException {
            public CorePropertyNotMatchedException(CoreProperty coreProperty) : this(coreProperty, null) { }
            /// <summary>
            /// TODO: Error message is no longer relevant since we now (Jan 2017) silently map missing CoreProperty-enums to the next integer values for TProperty
            /// </summary>
            /// <param name="coreProperty"></param>
            /// <param name="message"></param>
            public CorePropertyNotMatchedException(CoreProperty coreProperty, string message) : base(
                nameof(CoreProperty) + "." + coreProperty.ToString() + " does not have a matching " +
                nameof(CoreProperty) + " (" + typeof(CoreProperty) + ") value defined. " +
                "Resolution: You must define either " + typeof(CoreProperty) + "." + coreProperty.ToString() + " or " +
                "define a " + typeof(CoreProperty) + " with " + nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.CoreProperty) + " = " + nameof(CoreProperty) + "." + coreProperty.ToString() +
                (string.IsNullOrEmpty(message) ? "" : (" (Additional explanation: " + message + ")"))) { }
        }
    }

    /// <summary>
    /// Simple container class for <see cref="CoreProperty"/> and <see cref="AgoRapideAttributeT"/>
    /// TODO: Make immutable
    /// </summary>
    public class CPA {
        public CoreProperty cp;
        public AgoRapideAttributeT a;
    }

}
