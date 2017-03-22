using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.ComponentModel;
using AgoRapide.API;
using AgoRapide.Database;

namespace AgoRapide.Core {
    public static class Extensions {

        public static Property<TProperty> GetOrAddIsManyParent<TProperty>(this Dictionary<TProperty, Property<TProperty>> dict, TProperty p) where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var a = p.GetAgoRapideAttribute().A; a.AssertIsMany();
            if (dict.TryGetValue(p, out var retval)) {
                retval.AssertIsManyParent();
            } else {
                retval = dict[p] = Property<TProperty>.CreateIsManyParent(p);
            }
            return retval;
        }

        public static void AssertExactOne<T>(this List<T> list, Func<string> detailer) {
            if (list.Count != 1) throw new InvalidCountException("Expected exact 1 item in list but got " + list.Count + detailer.Result(".Details: "));
        }

        public static void AddValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) => AddValue(dictionary, key, value, null);
        /// <summary>
        /// Gives better error messages when adding a value to a directory if key already exists
        /// 
        /// Note how <see cref="AddValue2"/> is more preferable than <see cref="AddValue"/>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="detailer"></param>
        public static void AddValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value, Func<string> detailer) {
            if (dictionary.ContainsKey(key)) throw new KeyAlreadyExistsException("Key '" + key.ToString() + "' does already exist in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString() + detailer.Result("\r\nDetails: "));
            dictionary.Add(key, value);
        }

        public static void AddValue2<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : struct, IFormattable, IConvertible, IComparable => AddValue2(dictionary, key, value, null); // What we really would want is "where T : Enum"
        /// <summary>
        /// Gives better error messages when adding a value to a directory if key already exists
        /// 
        /// Note how <see cref="AddValue2"/> is more preferable than <see cref="AddValue"/>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="details"></param>
        public static void AddValue2<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value, Func<string> details) where TKey : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (dictionary.ContainsKey(key)) throw new KeyAlreadyExistsException("Key " + key.GetAgoRapideAttribute().PExplained + " does already exist in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString2() + details.Result(", Details: "));
            dictionary.Add(key, value);
        }

        /// <summary>
        /// Same as inbuilt LINQ's Single except that gives a more friendly exception message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <param name="predicateDescriber">May be null (but in that case you may as well use inbuilt LINQ's method)</param>
        /// <returns></returns>
        public static T Single<T>(this IEnumerable<T> source, Func<T, bool> predicate, Func<string> predicateDescriber) {
            try {
                return source.Single(predicate);
            } catch (Exception ex) {
                throw new SingleObjectNotFoundOrMultipleFoundException(predicateDescriber.Result(nameof(predicateDescriber) + ": "), ex);
            }
        }

        /// <summary>
        /// Same as inbuilt LINQ's SingleOrDefault except that gives a more friendly exception message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <param name="predicateDescriber">May be null (but in that case you may as well use inbuilt LINQ's method)</param>
        /// <returns></returns>
        public static T SingleOrDefault<T>(this IEnumerable<T> source, Func<T, bool> predicate, Func<string> predicateDescriber) {
            try {
                return source.SingleOrDefault(predicate);
            } catch (Exception ex) {
                throw new MultipleObjectsFoundException(predicateDescriber.Result(nameof(predicateDescriber) + ": "), ex);
            }
        }

        public static T GetValue<T>(this List<T> list, int index) => GetValue(list, index, null);
        /// <summary>
        /// Gives better error messages when accessing list with index out of range
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetValue<T>(this List<T> list, int index, Func<string> detailer) {
            if (list == null) throw new NullReferenceException(nameof(list) + detailer.Result("\r\nDetails: "));
            if (index >= list.Count) throw new IndexOutOfRangeException(nameof(index) + ": " + index + ", " + list.ListAsString() + detailer.Result("\r\nDetails: "));
            return list[index];
        }

        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) => GetValue(dictionary, key, null);
        /// <summary>
        /// Gives better error messages when reading value from directory if key does not exist
        /// 
        /// Note how <see cref="GetValue2"/> is more preferable than <see cref="GetValue"/>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="detailer">
        /// May be null
        /// Used to give details in case of an exception being thrown
        /// </param>
        /// <returns></returns>
        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<string> detailer) {
            if (dictionary == null) throw new NullReferenceException(nameof(dictionary) + detailer.Result("\r\nDetails: "));
            return dictionary.TryGetValue(key, out var retval) ? retval : throw new KeyNotFoundException("Key '" + key.ToString() + "' not found in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString() + detailer.Result("\r\nDetails: "));
        }

        public static TValue GetValue2<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TKey : struct, IFormattable, IConvertible, IComparable => GetValue2(dictionary, key, null); // What we really would want is "where T : Enum"
        /// <summary>
        /// Gives better error messages when reading value from directory if key does not exist
        /// 
        /// Note how <see cref="GetValue2"/> is more preferable than <see cref="GetValue"/>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="detailer">
        /// May be null
        /// Used to give details in case of an exception being thrown
        /// </param>
        /// <returns></returns>
        public static TValue GetValue2<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<string> detailer) where TKey : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (dictionary == null) throw new NullReferenceException(nameof(dictionary) + detailer.Result("\r\nDetails: "));
            return dictionary.TryGetValue(key, out var retval) ? retval : throw new KeyNotFoundException("Key '" + key.ToString() + "' not found in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString2() + detailer.Result("\r\nDetails: "));
        }

        public static string ListAsString<T>(this List<T> list) {
            if (list.Count > 100) return nameof(list) + ".Count: " + list.Count;
            return nameof(list) + ": " + string.Join(", ", list.Select(i => i.ToString()));
        }

        /// <summary>
        /// Gives a compressed overview of keys in a dictionary. Helpful for building exception messages. 
        /// 
        /// Note how <see cref="KeysAsString2"/> is more preferable than <see cref="KeysAsString"/>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static string KeysAsString<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) {
            if (dictionary.Count > 100) return "Keys.Count: " + dictionary.Keys.Count;
            return "Keys:\r\n" + string.Join(",\r\n", dictionary.Keys);
        }

        /// <summary>
        /// Gives a compressed overview of keys in a dictionary. Helpful for building exception messages. 
        /// 
        /// Note how <see cref="KeysAsString2"/> is more preferable than <see cref="KeysAsString"/>
        /// 
        /// Use this instead of <see cref="KeysAsString"/> whenever possible in order to get silently mapped <see cref="CoreProperty"/>-values better presented. 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static string KeysAsString2<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) where TKey : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (dictionary.Count > 100) return "Keys.Count: " + dictionary.Keys.Count;
            return "Keys:\r\n" + string.Join(",\r\n", dictionary.Keys.Select(k => k.GetAgoRapideAttribute().PExplained));
        }

        /// <summary>
        /// Useful when we want to write collection.ForEach( ... ) instead of collection.ToList().ForEach ( ... ) 
        ///
        /// Discussion about this at
        /// http://blogs.msdn.com/b/ericlippert/archive/2009/05/18/foreach-vs-foreach.aspx
        /// (short version http://stackoverflow.com/questions/10299458/is-the-listt-foreach-method-gone )
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action) {
            foreach (var t in collection) action(t);
        }

        private static ConcurrentDictionary<Type, string> _toStringShortCache = new ConcurrentDictionary<Type, string>();
        /// <summary>
        /// Gives a short representation of type as string. 
        /// Leaves out namespace, assembly and simplifies generic representation to look more like C# code. 
        /// Typical result: ApplicationPart&lt;P&gt;
        /// 
        /// See also <see cref="ToStringDB(Type)"/> for a string representation compatible with <see cref="Util.TryGetTypeFromString"/>. 
        /// 
        /// See also <see cref="ToStringVeryShort(Type)"/>. 
        /// 
        /// TODO: We would maybe also want to shorten OuterClass+InnerClass down to InnerClass here. 
        /// 
        /// Note how caches result since operation is somewhat complicated due do generic types. 
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static string ToStringShort(this Type type) => _toStringShortCache.GetOrAdd(type, t => {
            var retval = new System.Text.StringBuilder(); var maybe = new System.Text.StringBuilder();
            t.ToString().
                Replace("`1[", "<").
                Replace("`2[", "<").
                Replace("`3[", "<").
                Replace("`1+", "+").  // Necessary for 
                Replace("`2+", "+").  // inner classes like 
                Replace("`3+", "+").  // "AgoRapide.Database.PostgreSQLDatabase`1+PostgreSQLDatabaseException[AgoRapideSample.P]"
                Replace("[", "<").
                Replace("]", ">").ForEach(c => {
                    switch (c) {
                        case '.': maybe.Clear(); break; // Remove everything found
                        case ',': // Comma is for multiple generic arguments
                        case '<':
                        case '>': retval.Append(maybe.ToString() + c); maybe.Clear(); break;
                        default: maybe.Append(c); break;
                    }
                });
            return retval.ToString() + maybe.ToString();
        });

        private static ConcurrentDictionary<Type, string> _toStringVeryShortCache = new ConcurrentDictionary<Type, string>();
        /// <summary>
        /// Gives a very short representation of type as string. 
        /// Returns <see cref="ToStringShort"/> but without any generics information at all. 
        /// The representation is typical used for route templates for <see cref="APIMethodOrigin.Autogenerated"/> <see cref="APIMethod{TProperty}"/> 
        /// for instance in order to create something like Person/{id}.
        /// Note how <see cref="BaseEntityT{TProperty}"/> will be returned as "Entity" only.
        /// 
        /// Note how caches result since operation is somewhat complicated due do generic types. 
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static string ToStringVeryShort(this Type type) => _toStringVeryShortCache.GetOrAdd(type, t => t.ToStringShort().Split('<')[0].Replace("BaseEntityT", "Entity"));

        private static ConcurrentDictionary<Type, string> _toStringDBCache = new ConcurrentDictionary<Type, string>();
        /// <summary>
        /// Gives the minimum representation of type as string suitable for later reconstruction by <see cref="Type.GetType(string)"/> 
        /// (that is with namespace and including the simple assembly name but nothing more)
        /// 
        /// The return value is considered SQL "safe" in the sense that it may be inserted directly into an SQL statement. 
        /// 
        /// <see cref="Extensions.ToStringDB"/> corresponds to <see cref="Util.TryGetTypeFromString"/>
        /// 
        /// See also <see cref="ToStringShort(Type)"/> and <see cref="ToStringVeryShort(Type)"/>
        ///
        /// Example of return value is:
        ///   ApplicationPart&lt;P&gt; : AgoRapide.ApplicationPart`1[AgoRapideSample.P,AgoRapideSample],AgoRapide
        /// (the part before : is what is returned by <see cref="ToStringShort(Type)"/> and serves as a human readable shorthand)
        /// 
        /// Note complexities involved around generic types where the generic argument exist in a different assembly.
        /// For instance for 
        ///    typeof(AgoRapide.ApplicationPart{AgoRapideSample.P}) 
        /// where ApplicationPart resides in AgoRapide and
        /// P resides in AgoRapideSample a simple approach like 
        ///   <see cref="Type.ToString"/> + "," + <see cref="Type.Assembly"/>.<see cref="System.Reflection.Assembly.GetName"/> 
        /// would give
        ///   AgoRapide.ApplicationPart`1[AgoRapideSample.P],AgoRapide
        /// which is not enough information for <see cref="Type.GetType(string)"/>
        ///             
        /// Therefore, for types where generic arguments exist in a different assembly we have to do like explained here: 
        /// http://stackoverflow.com/questions/2276384/loading-a-generic-type-by-name-when-the-generics-arguments-come-from-multiple-as
        /// 
        /// In our case what is returned by this methdod (<see cref="ToStringDB(Type)"/> is something like mentioned above
        ///   AgoRapide.ApplicationPart`1[AgoRapideSample.P,AgoRapideSample],AgoRapide
        /// that is, we insert the assembly name where the type of the generic parameter resides, into the string.
        /// 
        /// Note how caches result since operation is somewhat complicated.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ToStringDB(this Type type) => _toStringDBCache.GetOrAdd(type, t => {
            var retval = t.ToString() + "," + t.Assembly.GetName().Name;
            if (!t.IsGenericType) return ToStringShort(t) + " : " + retval;
            var arguments = t.GetGenericArguments();
            if (arguments.Length != 1) throw new InvalidTypeException(type, nameof(t.GetGenericArguments) + ".Length != 1. Handling if this is just not implemented yet.");
            if (retval.Split(']').Length != 2) throw new InvalidTypeException(type, retval + " does not contain exactly one ]");
            return ToStringShort(t) + " : " + retval.Replace("]", "," + arguments[0].Assembly.GetName().Name + "]");
        });


        /// <summary>
        /// Convenience method for presenting decimal numbers. Facilitates easy use of decimal point consequently throughout the application
        /// </summary>
        /// <param name="_double"></param>
        /// <returns></returns>
        public static string ToString2(this double _double) => _double.ToString("0.00").Replace(",", ".");

        public static string ToSQLString(this Operator _operator) {
            switch (_operator) {
                case Operator.EQ: return "=";
                case Operator.GT: return ">";
                case Operator.LT: return "<";
                case Operator.GEQ: return ">=";
                case Operator.LEQ: return "<=";
                case Operator.LIKE: return "LIKE";
                case Operator.ILIKE: return "ILIKE";
                // TODO: Support IS (like IS NULL)
                default: throw new InvalidEnumException(_operator);
            }
        }
        public static Dictionary<Type, HashSet<Operator>> ValidOperatorsForType = new Dictionary<Type, HashSet<Operator>> {
            { typeof(long), new HashSet<Operator> { Operator.EQ, Operator.GT, Operator.LT, Operator.GEQ, Operator.LEQ } },
            { typeof(double), new HashSet<Operator> { Operator.EQ, Operator.GT, Operator.LT, Operator.GEQ, Operator.LEQ } },
            { typeof(DateTime), new HashSet<Operator> { Operator.EQ, Operator.GT, Operator.LT, Operator.GEQ, Operator.LEQ } },
            { typeof(bool), new HashSet<Operator> { Operator.EQ } },
            { typeof(string), new HashSet<Operator> { Operator.EQ, Operator.LIKE, Operator.ILIKE } },
            { typeof(List<long>), new HashSet<Operator> { Operator.IN } },
            { typeof(List<double>), new HashSet<Operator> { Operator.IN } },
            { typeof(List<DateTime>), new HashSet<Operator> { Operator.IN } },
            { typeof(List<bool>), new HashSet<Operator> { Operator.IN} },
            { typeof(List<string>), new HashSet<Operator> { Operator.IN } },
        };
        public static void AssertValidForType(this Operator _operator, Type type, Func<string> detailer) {
            if (!ValidOperatorsForType.TryGetValue(type, out var validOperators)) throw new InvalidEnumException(_operator, "Not valid for " + type + ". " + nameof(type) + " not recognized at all" + detailer.Result(". Details: "));
            if (!validOperators.Contains(_operator)) throw new InvalidEnumException(_operator, "Invalid for " + type + ". Valid operators are " + string.Join(", ", validOperators.Select(o => o.ToString())) + detailer.Result(". Details: "));
        }

        public static string ToString(this DateTime dateTime, DateTimeFormat resolution) {
            switch (resolution) {
                case DateTimeFormat.None:
                case DateTimeFormat.DateHourMinSecMs: return dateTime.ToString(Util.Configuration.DateAndHourMinSecMsFormat);
                case DateTimeFormat.DateHourMinSec: return dateTime.ToString(Util.Configuration.DateAndHourMinSecFormat);
                case DateTimeFormat.DateHourMin: return dateTime.ToString(Util.Configuration.DateAndHourMinFormat);
                case DateTimeFormat.DateOnly: return dateTime.ToString(Util.Configuration.DateOnlyFormat);
                default: throw new InvalidEnumException(resolution);
            }
        }

        /// <summary>
        /// Returns empty string if function is null
        /// </summary>
        /// <param name="function"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static string Result(this Func<string> function, string header) {
            if (function == null) return "";
            return header + function();
        }

        /// <summary>
        /// Returns all <typeparamref name="TProperty"/> for which <paramref name="type"/> is in <see cref="AgoRapideAttribute.Parents"/> 
        /// for which <paramref name="currentUser"/> have access.
        /// 
        /// Has practical use in <see cref="BaseEntityT{TProperty}.ToHTMLDetailed"/> and <see cref="BaseEntityT{TProperty}.ToJSONEntity"/> 
        /// (through <see cref="BaseEntityT{TProperty}.GetExistingProperties(BaseEntityT{TProperty}, AccessType)"/>)
        /// 
        /// Implements <see cref="AccessLocation.Entity"/>, <see cref="AccessLocation.Type"/> and <see cref = "AccessLocation.Property"/> 
        /// (and partly <see cref="AccessLocation.Relation"/>)
        /// </summary>
        /// <typeparam name="TProperty">
        /// Must be the same for every call for the same <paramref name="type"/> (because the cache-key is 
        /// only based on <paramref name="type"/>), not <typeparamref name="TProperty"/>).
        /// </typeparam>
        /// <param name="type"></param>
        /// <param name="currentUser">May be null in which case <see cref="AccessLevel.Anonymous"/> will be assumed</param>
        /// <param name="entity"></param>
        /// <param name="accessType"></param>
        /// <param name="accessLevelGiven"></param>
        /// <returns></returns>
        public static Dictionary<TProperty, AgoRapideAttributeT<TProperty>> GetChildPropertiesForUser<TProperty>(this Type type, BaseEntityT<TProperty> currentUser, BaseEntityT<TProperty> entity, AccessType accessType) where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var accessLevelGiven = currentUser?.AccessLevelGiven ?? AccessLevel.Anonymous;

            if (currentUser != null && currentUser.Id == entity.Id) {
                /// Partly implementing <see cref="AccessLocation.Relation"/>.
                /// 
                /// OK. Entity is supposed to be able to change "itself". 
                /// (see also <see cref="IDatabase{TProperty}.TryGetEntities"/> which has the main responsibility for <see cref="AccessLocation.Relation"/>). 
            } else {
                /// Implementing <see cref="AccessLocation.Entity"/>.
                var aType = type.GetAgoRapideAttribute(); // Used to get default values

                switch (accessType) {
                    case AccessType.Read: 
                        if (accessLevelGiven < entity.PV(GetCorePropertyMapper<TProperty>().Map(CoreProperty.AccessLevelRead), aType.AccessLevelRead)) {
                            /// TODO: Implement access through relations (not implemented as of March 2017)
                            return new Dictionary<TProperty, AgoRapideAttributeT<TProperty>>();
                        }
                        break;
                    case AccessType.Write: 
                        if (accessLevelGiven < entity.PV(GetCorePropertyMapper<TProperty>().Map(CoreProperty.AccessLevelWrite), aType.AccessLevelRead)) {
                            /// TODO: Implement access through relations (not implemented as of March 2017)
                            return new Dictionary<TProperty, AgoRapideAttributeT<TProperty>>();
                        }
                        break;
                    default: throw new InvalidEnumException(accessType);
                }
            }
            /// Implementing <see cref="AccessLocation.Type"/> and <see cref="AccessLocation.Property"/>.
            return GetChildPropertiesForAccessLevel<TProperty>(type, accessType, accessLevelGiven);
        }

        /// <summary>
        /// key = type.ToString() + "_" + accessType + "_" + accessLevelGiven
        /// </summary>
        private static ConcurrentDictionary<string, object> _allChildPropertiesForAccessLevel = new ConcurrentDictionary<string, object>();
        /// <summary>
        /// Returns all <typeparamref name="TProperty"/> for which <paramref name="type"/> is in <see cref="AgoRapideAttribute.Parents"/> 
        /// relevant for <paramref name="accessType"/> and <paramref name="accessLevelGiven"/>. 
        /// 
        /// Implements <see cref="AccessLocation.Type"/> and <see cref = "AccessLocation.Property"/> 
        /// For consideration of <see cref="AccessLocation.Entity"/> see 
        /// <see cref="GetChildPropertiesForUser"/>. 
        /// </summary>
        /// <typeparam name="TProperty">
        /// Must be the same for every call for the same <paramref name="type"/> (because the cache-key is 
        /// only based on <paramref name="type"/>), not <typeparamref name="TProperty"/>).
        /// </typeparam>
        /// <param name="type"></param>
        /// <param name="accessType"></param>
        /// <param name="accessLevelGiven"></param>
        /// <returns></returns>
        public static Dictionary<TProperty, AgoRapideAttributeT<TProperty>> GetChildPropertiesForAccessLevel<TProperty>(this Type type, AccessType accessType, AccessLevel accessLevelGiven) where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var key = type.ToString() + "_" + accessType + "_" + accessLevelGiven;
            var temp = _allChildPropertiesForAccessLevel.GetOrAdd(key, k => {
                var r = new Dictionary<TProperty, AgoRapideAttributeT<TProperty>>();

                /// Implementing <see cref="AccessLocation.Type"/>. 
                var aType = type.GetAgoRapideAttribute();
                switch (accessType) { /// Check if access at all is allowed (Typically like <see cref="APIMethod{TProperty}"/>' <see cref="AgoRapideAttribute"/> having <see cref="AccessType.Write"/> set to <see cref="AccessLevel.System"/>)
                    case AccessType.Read: if (accessLevelGiven < aType.AccessLevelRead) return r; break;
                    case AccessType.Write: if (accessLevelGiven < aType.AccessLevelWrite) return r; break;
                    default: throw new InvalidEnumException(accessType, key);
                }

                /// Implementing <see cref="AccessLocation.Property"/> for TProperty. 
                GetChildProperties<TProperty>(type).ForEach(e => {
                    switch (accessType) {
                        case AccessType.Read: if (accessLevelGiven >= e.Value.A.AccessLevelRead) r.Add(e.Key, e.Value); break;
                        case AccessType.Write: if (accessLevelGiven >= e.Value.A.AccessLevelWrite) r.Add(e.Key, e.Value); break;
                        default: throw new InvalidEnumException(accessType, key);
                    }
                });
                return r;
            });
            return temp as Dictionary<TProperty, AgoRapideAttributeT<TProperty>> ?? throw new InvalidObjectTypeException(temp, typeof(Dictionary<TProperty, AgoRapideAttributeT<TProperty>>), "For each specific type, like in this case " + type.ToStringShort() + ", you must call " + nameof(GetObligatoryChildProperties) + " with the same " + nameof(TProperty) + " every time.\r\n" + nameof(key) + ": " + key);
        }

        private static ConcurrentDictionary<Type, object> _allObligatoryAgoRapideProperties = new ConcurrentDictionary<Type, object>();
        /// <summary>
        /// Returns all <typeparamref name="TProperty"/> for which <paramref name="type"/> is in <see cref="AgoRapideAttribute.Parents"/> 
        /// with <see cref="AgoRapideAttribute.IsObligatory"/>. 
        /// 
        /// Has practical use when calling <see cref="IDatabase{TProperty}.CreateEntity"/>
        /// </summary>
        /// <typeparam name="TProperty">
        /// Must be the same for every call for the same <paramref name="type"/> (because the cache-key is 
        /// only based on <paramref name="type"/>), not <typeparamref name="TProperty"/>).
        /// </typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Dictionary<TProperty, AgoRapideAttributeT<TProperty>> GetObligatoryChildProperties<TProperty>(this Type type) where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var temp = _allObligatoryAgoRapideProperties.GetOrAdd(type, t => GetChildProperties<TProperty>(type).Where(e => e.Value.A.IsObligatory).ToDictionary(e => e.Key, e => e.Value));
            return temp as Dictionary<TProperty, AgoRapideAttributeT<TProperty>> ?? throw new InvalidObjectTypeException(temp, typeof(Dictionary<TProperty, AgoRapideAttributeT<TProperty>>), "For each specific type, like in this case " + type.ToStringShort() + ", you must call " + nameof(GetObligatoryChildProperties) + " with the same " + nameof(TProperty) + " every time.");
        }

        private static ConcurrentDictionary<Type, object> _allChildProperties = new ConcurrentDictionary<Type, object>();
        /// <summary>
        /// Returns all <typeparamref name="TProperty"/> for which <paramref name="type"/> is in <see cref="AgoRapideAttribute.Parents"/>
        /// </summary>
        /// <typeparam name="TProperty">
        /// Must be the same for every call for the same <paramref name="type"/> (because the cache-key is 
        /// only based on <paramref name="type"/>), not <typeparamref name="TProperty"/>).
        /// </typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Dictionary<TProperty, AgoRapideAttributeT<TProperty>> GetChildProperties<TProperty>(this Type type) where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var temp = _allChildProperties.GetOrAdd(type, t => {
                var r = new Dictionary<TProperty, AgoRapideAttributeT<TProperty>>();
                Util.EnumGetValues<TProperty>().ForEach(e => {
                    var a = e.GetAgoRapideAttribute();
                    if (a.IsParentFor(type)) r.Add(e, a);
                });
                var cpm = new CorePropertyMapper<TProperty>();
                Util.EnumGetValues<CoreProperty>().ForEach(coreProperty => {
                    var e = cpm.Map(coreProperty);
                    if (r.ContainsKey(e)) return; // Already included
                    var a = e.GetAgoRapideAttribute();
                    if (a.IsParentFor(type)) r.Add(e, a);
                });
                return r;
            });
            return temp as Dictionary<TProperty, AgoRapideAttributeT<TProperty>> ?? throw new InvalidObjectTypeException(temp, typeof(Dictionary<TProperty, AgoRapideAttributeT<TProperty>>), "For each specific type, like in this case " + type.ToStringShort() + ", you must call " + nameof(GetObligatoryChildProperties) + " with the same " + nameof(TProperty) + " every time.");
        }

        private static object _corePropertyMapper;
        /// <summary>
        /// </summary>
        /// <typeparam name="TProperty">Must be the same for every call because there is one cached value available</typeparam>
        /// <returns></returns>
        private static CorePropertyMapper<TProperty> GetCorePropertyMapper<TProperty>() where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (_corePropertyMapper != null) return _corePropertyMapper as CorePropertyMapper<TProperty> ?? throw new InvalidObjectTypeException(_corePropertyMapper, typeof(CorePropertyMapper<TProperty>), "You must call " + nameof(GetCorePropertyMapper) + " with the same " + nameof(TProperty) + " every time");
            return (CorePropertyMapper<TProperty>)(_corePropertyMapper = new CorePropertyMapper<TProperty>());
        }

        private static ConcurrentDictionary<Type, AgoRapideAttribute> _allAgoRapideAttributeForClass = new ConcurrentDictionary<Type, AgoRapideAttribute>();
        /// <summary>
        /// Returns <see cref="AgoRapideAttribute"/> for a class (or enum-"class") itself. 
        ///    
        /// Note use of caching. 
        /// 
        /// See <see cref="AgoRapideAttribute.GetAgoRapideAttribute(Type)"/> for documentation. 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static AgoRapideAttribute GetAgoRapideAttribute(this Type type) => _allAgoRapideAttributeForClass.GetOrAdd(type, t => AgoRapideAttribute.GetAgoRapideAttribute(t));
        // public static AgoRapideAttribute GetAgoRapideAttribute(this Type type) => _allAgoRapideAttributeForClass.GetOrAdd(type, t => (AgoRapideAttribute)(Attribute.GetCustomAttribute(t, typeof(AgoRapideAttribute)) ?? AgoRapideAttribute.GetNewDefaultInstance()));

        private static ConcurrentDictionary<Type, ConcurrentDictionary<string, AgoRapideAttribute>> allAgoRapideAttributeForEnum = new ConcurrentDictionary<Type, ConcurrentDictionary<string, AgoRapideAttribute>>();
        /// <summary>
        /// Returns <see cref="AgoRapideAttribute"/> for <paramref name="_enum"/>.
        /// 
        /// Note that this method gets ONLY the <see cref="AgoRapideAttribute"/> (instead of the enriched <see cref="AgoRapideAttributeT{TProperty}"/>)
        /// for an individual <paramref name="_enum"/>. This is only relevant when you do not posess the generic parameter necessary for 
        /// calling <see cref="GetAgoRapideAttribute{T}"/> extension method. 
        /// In other words, do not use this method but use the more sophisticated method<see cref="GetAgoRapideAttribute{T}"/> if possible.
        /// (since that method again returns a much more sophisticated object back)
        /// 
        /// Note use of caching
        /// </summary>
        /// <param name="_enum"></param>
        /// <returns></returns>
        public static AgoRapideAttribute GetAgoRapideAttribute(this object _enum) {
            var type = _enum.GetType();
            if (!type.IsEnum) throw new NotOfTypeEnumException(type);
            return allAgoRapideAttributeForEnum.
                GetOrAdd(_enum.GetType(), dummy => new ConcurrentDictionary<string, AgoRapideAttribute>()).
                GetOrAdd(_enum.ToString(), dummy => AgoRapideAttribute.GetAgoRapideAttribute(_enum, corePropertyAttributeGetter: () => null));
        }

        private static ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> allAgoRapideAttributeTForEnum = new ConcurrentDictionary<Type, ConcurrentDictionary<string, object>>();
        /// <summary>
        /// Returns <see cref="AgoRapideAttributeT{TProperty}"/> for enum value. 
        /// 
        /// Note use of caching
        /// 
        /// Recursivity warning: Note how <see cref="Extensions.GetAgoRapideAttribute{T}"/> uses <see cref="CorePropertyMapper{TProperty}.dict"/> 
        /// which again uses <see cref="Extensions.GetAgoRapideAttribute{T}"/>. This recursive call is OK as long as 
        /// <see cref="CorePropertyMapper{TProperty}.dict"/> only calls <see cref="Extensions.GetAgoRapideAttribute{T}"/> with
        /// defined values for <see cref="TProperty"/>.
        /// </summary>
        /// <typeparam name="T">
        /// Will often be the same as TProperty as used elsewhere in the library but since this
        /// method is useful for any kind of enums, the more generic name T is used</typeparam>
        /// <param name="_enum"></param>
        /// <returns></returns>
        public static AgoRapideAttributeT<T> GetAgoRapideAttribute<T>(this T _enum) where T : struct, IFormattable, IConvertible, IComparable => // What we really would want is "where T : Enum"
            (AgoRapideAttributeT<T>)allAgoRapideAttributeTForEnum.
            GetOrAdd(typeof(T), dummy => new ConcurrentDictionary<string, object>()).
            GetOrAdd(_enum.ToString(), dummy => {
                return new AgoRapideAttributeT<T>(AgoRapideAttribute.GetAgoRapideAttribute(_enum, new Func<AgoRapideAttribute>(() => {
                    if (_enum.GetType().Equals(typeof(CoreProperty))) throw new InvalidTypeException(_enum.GetType(), "Can not be of type " + typeof(CoreProperty));
                    // See recursivity warning above.
                    return new CorePropertyMapper<T>().TryMapReverse(_enum, out var coreProperty) ? AgoRapideAttribute.GetAgoRapideAttribute(coreProperty, () => null) : null;
                })),
                _enum.GetType().GetField(_enum.ToString()) == null ? _enum : (T?)null /// Mark that silently mapped <see cref="CoreProperty"/> if not exists
                );
            });

        public static string Extract(this string text, string start, string end) => TryExtract(text, start, end, out var retval) ? retval : throw new InvalidExtractException(text, start, end);
        public class InvalidExtractException : ApplicationException {
            public InvalidExtractException(string text, string start, string end) : base("Unable to extract from '" + text + "' between '" + start + "' and '" + end + "'") { }
        }

        /// <summary>
        /// Equivalent to <see cref="string.Replace(string, string)"/> but with the addition of asserting
        /// that <paramref name="oldValue"/> really occurs within <paramref name="text"/> at least once. 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue">Must be different from <paramref name="oldValue"/></param>
        /// <returns></returns>
        public static string ReplaceWithAssert(this string text, string oldValue, string newValue) {
            var retval = text.Replace(oldValue, newValue);
            if (retval.Equals(text)) throw new InvalidReplaceException(text, oldValue, newValue);
            return retval;
        }
        public class InvalidReplaceException : ApplicationException {
            public InvalidReplaceException(string text, string oldValue, string newValue) : base(nameof(oldValue) + "\r\n\r\n" + oldValue + "\r\n\r\nnot found, unable to replace with " + nameof(newValue) + "\r\n\r\n" + newValue + "\r\n\r\nwithin\r\n\r\n" + text) { }
        }

        /// <summary>
        /// Extracts content between first occurence of <paramref name="start"/> and <paramref name="end"/>
        /// Returns false if does not find <paramref name="start"/> or <paramref name="end"/>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static bool TryExtract(this string text, string start, string end, out string retval) {
            if (string.IsNullOrEmpty(start)) throw new ArgumentNullException(nameof(start) + (start == null ? "" : ".IsEmpty"));
            if (string.IsNullOrEmpty(end)) throw new ArgumentNullException(nameof(end) + (end == null ? "" : ".IsEmpty"));
            var startPos = text?.IndexOf(start) ?? throw new ArgumentNullException(nameof(text));
            if (startPos == -1) { retval = null; return false; }
            var endPos = text.IndexOf(end, startPos + start.Length);
            if (endPos == -1) { retval = null; return false; }
            retval = text.Substring(startPos + start.Length, endPos - startPos - start.Length);
            return true;
        }

        /// <summary>
        /// Makes "safe" attempt of turning an entity id into <see cref="BaseEntity.Name"/> through <see cref="Util.EntityCache"/>
        /// 
        /// Note possible security consequences of this method. Use sparingly or not at all if in doubt.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string AsEntityName(this long id) => id <= 0 ? "[NOT SET]" : (Util.EntityCache.TryGetValue(id, out var e) ? e.Name : id.ToString());

        public static List<string> Split(this string _string, string separator) => _string.Split(new string[] { separator }, StringSplitOptions.None).ToList();

        public static string HTMLEncode(this string _string) => System.Net.WebUtility.HtmlEncode(_string);
        /// <summary>
        /// 1) Adds {br} to every new line
        /// 2) Turns into hyperlinks if <paramref name="_string"/>.StartsWith "http?//"
        /// 
        /// TODO: Add other enhancements
        /// </summary>
        /// <param name="_string"></param>
        /// <returns></returns>
        public static string HTMLEncodeAndEnrich<TProperty>(this string _string, Request<TProperty> request) where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (_string.StartsWith("http://") || _string.StartsWith("https://")) {
                return string.Join("\r\n<br>", _string.Split("\r\n").Select(s => "<a href=\"" + s + (request.ResponseFormat == ResponseFormat.HTML && !s.EndsWith(Util.Configuration.HTMLPostfixIndicator) ? Util.Configuration.HTMLPostfixIndicator : "") + "\">" + s.HTMLEncode() + "</a>"));
            }
            return HTMLEncode(_string).Replace("\r\n", "\r\n<br>");
        }

        /// <summary>
        /// Convenience method that shortens down code in cases where an instance of an object must be 
        /// created first in order to use that same variable multiple times
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="action"></param>
        public static void Use<T>(this T obj, Action<T> action) => action(obj);
        public static T2 Use<T1, T2>(this T1 obj, Func<T1, T2> func) => func(obj);
    }
}