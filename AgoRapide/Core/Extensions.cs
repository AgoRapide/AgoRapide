using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.ComponentModel;
using AgoRapide.API;
using AgoRapide.Database;

namespace AgoRapide.Core {
    public static class Extensions {

        /// <summary>
        /// Readies <paramref name="dict"/> for storing of <see cref="PropertyKeyAttribute.IsMany"/> <paramref name="key"/> properties 
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Property GetOrAddIsManyParent(this Dictionary<CoreP, Property> dict, PropertyKey key) {
            key.Key.A.AssertIsMany(null);
            if (dict.TryGetValue(key.Key.CoreP, out var retval)) {
                retval.AssertIsManyParent();
            } else {
                retval = dict[key.Key.CoreP] = Property.CreateIsManyParent(key);
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

        public static void AddValue<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue value) => AddValue(dictionary, key, value, null);
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
        public static void AddValue<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue value, Func<string> detailer) {
            if (dictionary.ContainsKey(key)) throw new KeyAlreadyExistsException("Key '" + key.ToString() + "' does already exist in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString() + detailer.Result("\r\nDetails: "));
            dictionary[key] = value;
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
            if (dictionary.ContainsKey(key)) throw new KeyAlreadyExistsException("Key " + key.GetEnumValueAttribute().EnumValueExplained + " does already exist in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString2() + details.Result(", Details: "));
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
        /// Note how <see cref="GetValue2"/> is more preferable than <see cref="GetValue"/> due to restrictions on <typeparamref name="TKey"/>
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

        public static TValue GetValue<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key) => GetValue(dictionary, key, null);
        /// <summary>
        /// Gives better error messages when reading value from directory if key does not exist
        /// 
        /// Note how <see cref="GetValue2"/> is more preferable than <see cref="GetValue"/> due to restrictions on <typeparamref name="TKey"/>
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
        public static TValue GetValue<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<string> detailer) {
            if (dictionary == null) throw new NullReferenceException(nameof(dictionary) + detailer.Result("\r\nDetails: "));
            return dictionary.TryGetValue(key, out var retval) ? retval : throw new KeyNotFoundException("Key '" + key.ToString() + "' not found in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString() + detailer.Result("\r\nDetails: "));
        }

        public static TValue GetValue2<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TKey : struct, IFormattable, IConvertible, IComparable => GetValue2(dictionary, key, null); // What we really would want is "where T : Enum"
        /// <summary>
        /// Gives better error messages when reading value from directory if key does not exist
        /// 
        /// Note how <see cref="GetValue2"/> is more preferable than <see cref="GetValue"/> due to restrictions on <typeparamref name="TKey"/>
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

        public static TValue GetValue2<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key) where TKey : struct, IFormattable, IConvertible, IComparable => GetValue2(dictionary, key, null);  // What we really would want is "where T : Enum"
        /// <summary>
        /// Gives better error messages when reading value from directory if key does not exist
        /// 
        /// Note how <see cref="GetValue2"/> is more preferable than <see cref="GetValue"/> due to restrictions on <typeparamref name="TKey"/>
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
        public static TValue GetValue2<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<string> detailer) where TKey : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
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
        /// Note how <see cref="KeysAsString2"/> is more preferable than <see cref="KeysAsString"/> due to restrictions on <typeparamref name="TKey"/>
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
        /// Note how <see cref="KeysAsString2"/> is more preferable than <see cref="KeysAsString"/> due to restrictions on <typeparamref name="TKey"/>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static string KeysAsString<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary) {
            if (dictionary.Count > 100) return "Keys.Count: " + dictionary.Keys.Count;
            return "Keys:\r\n" + string.Join(",\r\n", dictionary.Keys);
        }

        /// <summary>
        /// TODO: This does not work as intended after mapping TO <see cref="CoreP"/> in Mar 2017.
        /// 
        /// Gives a compressed overview of keys in a dictionary. Helpful for building exception messages. 
        /// 
        /// Note how <see cref="KeysAsString2"/> is more preferable than <see cref="KeysAsString"/> due to restrictions on <typeparamref name="TKey"/>
        /// 
        /// Use this instead of <see cref="KeysAsString"/> whenever possible in order to get silently mapped <see cref="CoreP"/>-values better presented. 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static string KeysAsString2<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) where TKey : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (dictionary.Count > 100) return "Keys.Count: " + dictionary.Keys.Count;
            if (!typeof(TKey).IsEnum) return "Keys:\r\n" + string.Join(",\r\n", dictionary.Keys.Select(k => k.ToString()));
            return "Keys:\r\n" + string.Join(",\r\n", dictionary.Keys.Select(k => k.GetEnumValueAttribute().EnumValueExplained));
        }

        /// <summary>
        /// TODO: This does not work as intended after mapping TO <see cref="CoreP"/> in Mar 2017.
        /// 
        /// Gives a compressed overview of keys in a dictionary. Helpful for building exception messages. 
        /// 
        /// Note how <see cref="KeysAsString2"/> is more preferable than <see cref="KeysAsString"/> due to restrictions on <typeparamref name="TKey"/>
        /// 
        /// Use this instead of <see cref="KeysAsString"/> whenever possible in order to get silently mapped <see cref="CoreP"/>-values better presented. 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static string KeysAsString2<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary) where TKey : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (dictionary.Count > 100) return "Keys.Count: " + dictionary.Keys.Count;
            if (!typeof(TKey).IsEnum) return "Keys:\r\n" + string.Join(",\r\n", dictionary.Keys.Select(k => k.ToString()));
            return "Keys:\r\n" + string.Join(",\r\n", dictionary.Keys.Select(k => k.GetEnumValueAttribute().EnumValueExplained));
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
        /// The representation is typical used for route templates for <see cref="APIMethodOrigin.Autogenerated"/> <see cref="APIMethod"/> 
        /// for instance in order to create something like Person/{id}.
        /// Note how <see cref="BaseEntity"/> will be returned as "Entity" only.
        /// 
        /// Note how caches result since operation is somewhat complicated due do generic types. 
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static string ToStringVeryShort(this Type type) => _toStringVeryShortCache.GetOrAdd(type, t => t.ToStringShort().Split('<')[0].Replace("BaseEntity", "Entity"));

        private static ConcurrentDictionary<Type, string> _toStringDBCache = new ConcurrentDictionary<Type, string>();
        /// <summary>
        /// Gives the minimum representation of type as string suitable for later reconstruction by <see cref="Type.GetType(string)"/> 
        /// (that is with namespace and including all relevant assembly names but nothing more)
        /// 
        /// The return value is considered SQL "safe" in the sense that it may be inserted directly into an SQL statement. 
        /// 
        /// <see cref="Extensions.ToStringDB"/> corresponds to <see cref="Util.TryGetTypeFromString"/>
        /// 
        /// See also <see cref="ToStringShort(Type)"/> and <see cref="ToStringVeryShort(Type)"/>
        ///
        /// -----------------------------------------------------------------
        /// The historical reason for creating <see cref="Extensions.ToStringDB"/> and <see cref="Util.TryGetTypeFromString"/> was supporting
        /// generics where generic arguments exist in a different assembly. 
        /// After architectural change in March 2016 (mapping TO <see cref="CoreP"/> instead of FROM)
        /// a lot of unnecessary generic code was eliminated. This did in practice eliminiate the complexity present in these methods now.
        /// The following is therefore somewhat outdated:
        /// -----------------------------------------------------------------
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
        /// -----------------------------------------------------------------
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
                case DateTimeFormat.DateHourMinSecMs: return dateTime.ToString(Util.Configuration.A.DateAndHourMinSecMsFormat);
                case DateTimeFormat.DateHourMinSec: return dateTime.ToString(Util.Configuration.A.DateAndHourMinSecFormat);
                case DateTimeFormat.DateHourMin: return dateTime.ToString(Util.Configuration.A.DateAndHourMinFormat);
                case DateTimeFormat.DateOnly: return dateTime.ToString(Util.Configuration.A.DateOnlyFormat);
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
        /// Returns all <see cref="CoreP"/> for which <paramref name="type"/> is in <see cref="PropertyKeyAttribute.Parents"/> 
        /// and for which <paramref name="currentUser"/> have access.
        /// 
        /// Has practical use in <see cref="BaseEntity.ToHTMLDetailed"/> and <see cref="BaseEntity.ToJSONEntity"/> 
        /// (through <see cref="BaseEntity.GetExistingProperties(BaseEntity, AccessType)"/>)
        /// 
        /// Implements <see cref="AccessLocation.Entity"/>, <see cref="AccessLocation.Type"/> and <see cref = "AccessLocation.Property"/> 
        /// (and partly <see cref="AccessLocation.Relation"/>)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="currentUser">May be null in which case <see cref="AccessLevel.Anonymous"/> will be assumed</param>
        /// <param name="entity"></param>
        /// <param name="accessType"></param>
        /// <param name="accessLevelGiven"></param>
        /// <returns></returns>
        public static Dictionary<CoreP, PropertyKey> GetChildPropertiesForUser(this Type type, BaseEntity currentUser, BaseEntity entity, AccessType accessType) {
            var accessLevelGiven = currentUser?.AccessLevelGiven ?? AccessLevel.Anonymous;

            if (currentUser != null && currentUser.Id == entity.Id) {
                /// Partly implementing <see cref="AccessLocation.Relation"/>.
                /// 
                /// OK. Entity is supposed to be able to change "itself". 
                /// (see also <see cref="IDatabase.TryGetEntities"/> which has the main responsibility for <see cref="AccessLocation.Relation"/>). 
            } else {
                /// Implementing <see cref="AccessLocation.Entity"/>.
                var classAttribute = type.GetClassAttribute(); // Used to get default values

                switch (accessType) {
                    case AccessType.Read:
                        if (accessLevelGiven < entity.PV(CoreP.AccessLevelRead.A(), classAttribute.AccessLevelRead)) {
                            /// TODO: Implement access through relations (not implemented as of March 2017)
                            return new Dictionary<CoreP, PropertyKey>();
                        }
                        break;
                    case AccessType.Write:
                        if (accessLevelGiven < entity.PV(CoreP.AccessLevelWrite.A(), classAttribute.AccessLevelRead)) {
                            /// TODO: Implement access through relations (not implemented as of March 2017)
                            return new Dictionary<CoreP, PropertyKey>();
                        }
                        break;
                    default: throw new InvalidEnumException(accessType);
                }
            }
            /// Implementing <see cref="AccessLocation.Type"/> and <see cref="AccessLocation.Property"/>.
            return GetChildPropertiesForAccessLevel(type, accessType, accessLevelGiven);
        }

        /// <summary>
        /// key = type.ToString() + "_" + accessType + "_" + accessLevelGiven
        /// </summary>
        private static ConcurrentDictionary<string, Dictionary<CoreP, PropertyKey>> _childPropertiesForAccessLevelCache = new ConcurrentDictionary<string, Dictionary<CoreP, PropertyKey>>();
        /// <summary>
        /// Returns all <see cref="CoreP"/> relevant for <paramref name="accessType"/> and <paramref name="accessLevelGiven"/> 
        /// for which <paramref name="type"/> is in <see cref="PropertyKeyAttribute.Parents"/> 
        /// 
        /// Implements <see cref="AccessLocation.Type"/> and <see cref = "AccessLocation.Property"/> 
        /// For consideration of <see cref="AccessLocation.Entity"/> see <see cref="GetChildPropertiesForUser"/>. 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="accessType"></param>
        /// <param name="accessLevelGiven"></param>
        /// <returns></returns>
        public static Dictionary<CoreP, PropertyKey> GetChildPropertiesForAccessLevel(this Type type, AccessType accessType, AccessLevel accessLevelGiven) {
            var key = type.ToString() + "_" + accessType + "_" + accessLevelGiven;
            return _childPropertiesForAccessLevelCache.GetOrAdd(key, k => {
                var r = new Dictionary<CoreP, PropertyKey>();

                /// Implementing <see cref="AccessLocation.Type"/>. 
                var classAttribute = type.GetClassAttribute();
                switch (accessType) { /// Check if access at all is allowed (Typically like <see cref="APIMethod"/>' <see cref="PropertyKeyAttribute"/> having <see cref="AccessType.Write"/> set to <see cref="AccessLevel.System"/>)
                    case AccessType.Read: if (accessLevelGiven < classAttribute.AccessLevelRead) return r; break;
                    case AccessType.Write: if (accessLevelGiven < classAttribute.AccessLevelWrite) return r; break;
                    default: throw new InvalidEnumException(accessType, key);
                }

                /// Implementing <see cref="AccessLocation.Property"/> for TProperty. 
                GetChildProperties(type).ForEach(e => {
                    switch (accessType) {
                        case AccessType.Read: if (accessLevelGiven >= e.Value.Key.A.AccessLevelRead) r.Add(e.Key, e.Value); break;
                        case AccessType.Write: if (accessLevelGiven >= e.Value.Key.A.AccessLevelWrite) r.Add(e.Key, e.Value); break;
                        default: throw new InvalidEnumException(accessType, key);
                    }
                });
                return r;
            });
        }

        private static ConcurrentDictionary<Type, Dictionary<CoreP, PropertyKeyWithIndex>> _obligatoryChildPropertiesCache = new ConcurrentDictionary<Type, Dictionary<CoreP, PropertyKeyWithIndex>>();
        /// <summary>
        /// Returns all <see cref="CoreP"/> for which <paramref name="type"/> is in <see cref="PropertyKeyAttribute.Parents"/> 
        /// with <see cref="PropertyKeyAttribute.IsObligatory"/>. 
        /// 
        /// Note that <see cref="PropertyKeyAttribute.IsMany"/> combined with <see cref="PropertyKeyAttribute.IsObligatory"/> will result in <see cref="PropertyKeyWithIndex.Index"/> #1 being used
        /// 
        /// Has practical use when calling <see cref="IDatabase.CreateEntity"/>
        /// 
        /// Now how returns <see cref="PropertyKey.PropertyKeyWithIndex"/> from <see cref="GetChildProperties"/>.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Dictionary<CoreP, PropertyKeyWithIndex> GetObligatoryChildProperties(this Type type) => _obligatoryChildPropertiesCache.GetOrAdd(type, t => GetChildProperties(type).Where(e => e.Value.Key.A.IsObligatory).ToDictionary(e => e.Key, e => e.Value.Key.A.IsMany ? new PropertyKeyWithIndex(e.Value.Key, 1) : e.Value.PropertyKeyWithIndex));

        private static ConcurrentDictionary<Type, Dictionary<CoreP, PropertyKey>> _childPropertiesCache = new ConcurrentDictionary<Type, Dictionary<CoreP, PropertyKey>>();
        /// <summary>
        /// Returns all <see cref="CoreP"/> for which <paramref name="type"/> is in <see cref="PropertyKeyAttribute.Parents"/>. 
        /// Note how result is cached.
        /// TODO: Add reset of cache (because of <see cref="EnumMapper.GetA(string, IDatabase)"/> which will add new mappings after application startup)
        /// TODO: OR EVEN BETTER, MOVE INTO <see cref="EnumMapper"/> INSTEAD
        /// 
        /// TODO: As of Apr 2017 it looks like <see cref="EnumMapper.GetA(string, IDatabase)"/> is not going to be used after all
        /// TODO: (corresponding functionality has been put into <see cref="PostgreSQLDatabase"/>.ReadOneProperty instead.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Dictionary<CoreP, PropertyKey> GetChildProperties(this Type type) =>
             _childPropertiesCache.GetOrAdd(type, t => EnumMapper.AllCoreP.Where(key => key.Key.IsParentFor(type)).ToDictionary(key => key.Key.CoreP, key => key));

        private static ConcurrentDictionary<Type, ClassAttribute> _classAttributeCache = new ConcurrentDictionary<Type, ClassAttribute>();
        /// <summary>
        /// Note use of caching. 
        /// See <see cref="ClassAttribute.GetAttribute"/> for documentation. 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ClassAttribute GetClassAttribute(this Type type) => _classAttributeCache.GetOrAdd(type, t => ClassAttribute.GetAttribute(t));

        private static ConcurrentDictionary<Type, EnumAttribute> _enumAttributeCache = new ConcurrentDictionary<Type, EnumAttribute>();
        /// <summary>
        /// Note use of caching. 
        /// See <see cref="EnumAttribute.GetAttribute"/> for documentation. 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static EnumAttribute GetEnumAttribute(this Type type) => _enumAttributeCache.GetOrAdd(type, t => EnumAttribute.GetAttribute(t));

        private static ConcurrentDictionary<Type, ConcurrentDictionary<string, EnumValueAttribute>> _enumValueAttributeCache = new ConcurrentDictionary<Type, ConcurrentDictionary<string, EnumValueAttribute>>();
        public static EnumValueAttribute GetEnumValueAttribute(this object _enum) {
            var type = _enum.GetType();
            NotOfTypeEnumException.AssertEnum(type);
            return _enumValueAttributeCache.
                GetOrAdd(type, dummy => new ConcurrentDictionary<string, EnumValueAttribute>()).
                GetOrAdd(_enum.ToString(), dummy => EnumValueAttribute.GetAttribute(_enum));
        }

        private static ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyKeyAttribute>> _propertyKeyAttributeCache = new ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyKeyAttribute>>();
        /// <summary>
        /// Only used by <see cref="Property.ValueA"/>
        /// 
        /// Returns <see cref="PropertyKeyAttribute"/> for <paramref name="_enum"/>.
        /// 
        /// Note that this method gets ONLY the <see cref="PropertyKeyAttribute"/> (instead of the enriched <see cref="PropertyKeyAttributeEnriched"/>)
        /// for an individual <paramref name="_enum"/>. This is only relevant when you do not possess the generic parameter necessary for 
        /// calling <see cref="Extensions.GetPropertyKeyAttributeT"/>.
        /// In other words, normally do not use this method but use the more sophisticated method <see cref="Extensions.GetPropertyKeyAttributeT"/> whenever possible.
        /// (since that method again returns a much more sophisticated object back)
        /// </summary>
        /// <param name="_enum"></param>
        /// <returns></returns>
        public static PropertyKeyAttribute GetPropertyKeyAttribute(this object _enum) {
            var type = _enum.GetType();
            NotOfTypeEnumException.AssertEnum(type);
            return _propertyKeyAttributeCache.
                GetOrAdd(type, dummy => new ConcurrentDictionary<string, PropertyKeyAttribute>()).
                GetOrAdd(_enum.ToString(), dummy => PropertyKeyAttribute.GetAttribute(_enum));
        }

        ///// <summary>
        ///// TODO: Consider using ordinary dictionary and demand all enums to be registered at application startup.
        ///// TODO: OR: 
        ///// </summary>
        //private static ConcurrentDictionary<Type, Dictionary<int, PropertyKeyAttributeEnriched>> _agoRapideAttributeTCache = new ConcurrentDictionary<Type, Dictionary<int, PropertyKeyAttributeEnriched>>();
        ///// <summary>
        ///// Note use of caching
        ///// 
        ///// Note how we do not return a <see cref="PropertyKeyAttributeEnrichedT{T}"/> object because of <see cref="ReplaceAgoRapideAttribute{T}"/> 
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="_enum"></param>
        ///// <returns></returns>
        //public static PropertyKeyAttributeEnriched GetPropertyKeyAttributeT<T>(this T _enum) where T : struct, IFormattable, IConvertible, IComparable => // What we really would want is "where T : Enum"
        //    _agoRapideAttributeTCache.
        //        GetValue(typeof(T),() => "Possible cause: No call made to " +  nameof(SetPropertyKeyAttribute) + " for " + typeof(T)).
        //        //GetOrAdd(typeof(T), dummy => {
        //        //    throw new PropertyKey.InvalidPropertyKeyException("No )
        //            //NotOfTypeEnumException.AssertEnum(typeof(T));
        //            //return Util.EnumGetValues(exclude: (T)(object)-1). // Note how we also want the .None value (therefore exclude -1 below)
        //            //    ToDictionary(e => (int)(object)e, e => (PropertyKeyAttributeEnriched)(new PropertyKeyAttributeEnrichedT<T>(PropertyKeyAttribute.GetAttribute(e), null)));
        //        // }).
        //        GetValue2((int)(object)_enum, () => "Hint: Is " + _enum + " a valid value for " + typeof(T) + "?");

        ///// <summary>
        ///// Only to be called at application startup from <see cref="EnumMapper."/>. 
        ///// 
        ///// TODO: TRY TO REMOVE THIS!
        ///// </summary>
        ///// <param name="type"></param>
        ///// <param name="attributes"></param>
        ///// <param name="strict">If true then will throw exception if already exists.</param>
        //public static void SetPropertyKeyAttribute(Type type, Dictionary<int, PropertyKeyAttributeEnriched> attributes, bool strict) {
        //    if (strict && _agoRapideAttributeTCache.ContainsKey(type)) throw new KeyAlreadyExistsException(type, nameof(_agoRapideAttributeTCache), 
        //        "All -" + nameof(EnumType.PropertyKey) + "- are supposed to be found by " + nameof(EnumMapper.MapEnum) + " before " + nameof(GetPropertyKeyAttributeT) + " is being called");
        //    _agoRapideAttributeTCache[type] = attributes;
        //}

        public static PropertyKey A(this CoreP coreP) => EnumMapper.GetA(coreP);
        public static PropertyKey A(this DBField dbField) => EnumMapper.GetA(dbField);
        public static PropertyKey A(this ConfigurationAttribute.ConfigurationKey configurationKey) => EnumMapper.GetA(configurationKey);

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
        public static string HTMLEncodeAndEnrich(this string _string, Request request) {
            if (_string.StartsWith("http://") || _string.StartsWith("https://")) {
                return string.Join("\r\n<br>", _string.Split("\r\n").Select(s => "<a href=\"" + s + (request.ResponseFormat == ResponseFormat.HTML && !s.EndsWith(Util.Configuration.A.HTMLPostfixIndicator) ? Util.Configuration.A.HTMLPostfixIndicator : "") + "\">" + s.HTMLEncode() + "</a>"));
            }
            return HTMLEncode(_string).Replace("\r\n", "\r\n<br>");
        }

        /// <summary>
        /// Encloses <paramref name="html"/> within a HTML span with title <paramref name="tooltip"/>
        /// </summary>
        /// <param name="html"></param>
        /// <param name="tooltip">May be null or empty in which case only <paramref name="html"/> will be returned</param>
        /// <returns></returns>
        public static string HTMLEncloseWithinTooltip(this string html, string tooltip) => string.IsNullOrEmpty(tooltip) ? html : "<span title=\"" + tooltip.HTMLEncode() + "\">" + html + " (+)</span>";

        /// <summary>
        /// Convenience method that shortens down code in cases where an instance of an object must be 
        /// created first in order to use that same variable multiple times
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="action"></param>
        public static void Use<T>(this T obj, Action<T> action) => action(obj);
        public static T2 Use<T1, T2>(this T1 obj, Func<T1, T2> func) => func(obj);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="key"></param>
        /// <param name="detailer">May be null</param>
        /// <returns></returns>
        public static Property ToIsManyParent<T>(this List<T> list, BaseEntity parent, PropertyKey key, Func<string> detailer) => Util.ConvertListToIsManyParent(parent, key, list, detailer);
    }
}