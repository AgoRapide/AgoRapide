// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
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
        public static Property GetOrAddIsManyParent(this ConcurrentDictionary<CoreP, Property> dict, PropertyKey key) {
            key.Key.A.AssertIsMany(null);
            if (dict.TryGetValue(key.Key.CoreP, out var retval)) {
                retval.AssertIsManyParent();
            } else {
                retval = dict[key.Key.CoreP] = Property.CreateIsManyParent(key);
            }
            return retval;
        }

        /// <summary>
        /// TODO: Most probably not needed anymore after switch to ConcurrentDictionary Nov 2017
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        [ClassMember(Description =
            "Flattens (for one level only) any -" + nameof(PropertyKeyAttribute.IsMany) + "- found.\r\n" +
            "Useful before calling -" + nameof(BaseDatabase.CreateProperty) + "- / -" + nameof(BaseDatabase.UpdateProperty) + "-.")]
        public static List<Property> Flatten(this Dictionary<CoreP, Property> dict) {
            var retval = new List<Property>();
            dict.Values.ForEach(p => {
                if (p.Key.Key.A.IsMany) {
                    retval.AddRange(p.Properties.Values.ToList());
                } else {
                    retval.Add(p);
                }
            });
            return retval;
        }

        [ClassMember(Description =
            "Flattens (for one level only) any -" + nameof(PropertyKeyAttribute.IsMany) + "- found.\r\n" +
            "Useful before calling -" + nameof(BaseDatabase.CreateProperty) + "- / -" + nameof(BaseDatabase.UpdateProperty) + "-.")]
        public static List<Property> Flatten(this ConcurrentDictionary<CoreP, Property> dict) {
            var retval = new List<Property>();
            dict.Values.ForEach(p => {
                if (p.Key.Key.A.IsMany) {
                    retval.AddRange(p.Properties.Values.ToList());
                } else {
                    retval.Add(p);
                }
            });
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
            if (dictionary == null) throw new NullReferenceException(nameof(dictionary) + ", Key '" + key.ToString() + detailer.Result("\r\nDetails: "));
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
            if (dictionary == null) throw new NullReferenceException(nameof(dictionary) + ", Key '" + key.ToString() + detailer.Result("\r\nDetails: "));
            if (dictionary.ContainsKey(key)) throw new KeyAlreadyExistsException("Key '" + key.ToString() + "' does already exist in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString() + detailer.Result("\r\n---\r\nDetails: "));
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
        /// <param name="detailer"></param>
        public static void AddValue2<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value, Func<string> detailer) where TKey : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (dictionary == null) throw new NullReferenceException(nameof(dictionary) + ", Key '" + key.GetEnumValueAttribute().EnumValueExplained.ToString() + detailer.Result("\r\nDetails: "));
            if (dictionary.ContainsKey(key)) throw new KeyAlreadyExistsException("Key " + key.GetEnumValueAttribute().EnumValueExplained + " does already exist in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString2() + detailer.Result("\r\n---\r\nDetails: "));
            dictionary.Add(key, value);
        }

        public static void AddValue2<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : struct, IFormattable, IConvertible, IComparable => AddValue2(dictionary, key, value, null); // What we really would want is "where T : Enum"
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
        public static void AddValue2<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue value, Func<string> detailer) where TKey : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (dictionary == null) throw new NullReferenceException(nameof(dictionary) + ", Key '" + key.GetEnumValueAttribute().EnumValueExplained.ToString() + detailer.Result("\r\nDetails: "));
            if (!dictionary.TryAdd(key, value)) throw new KeyAlreadyExistsException("Key " + key.GetEnumValueAttribute().EnumValueExplained + " does already exist in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString2() + detailer.Result("\r\n---\r\nDetails: "));
        }

        /// <summary>
        /// Same as inbuilt LINQ's Single except that gives a more friendly exception message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <param name="predicateDescriber">May be null (but in that case you may as well use the inbuilt in .NET LINQ method)</param>
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

        /// <summary>
        /// Gives better error messages when accessing list with index out of range
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetValue<T>(this List<T> list, int index, Func<string> detailer = null) {
            if (list == null) throw new NullReferenceException(nameof(list) + detailer.Result("\r\nDetails: "));
            if (index >= list.Count) throw new IndexOutOfRangeException(nameof(index) + ": " + index + ", " + list.ListAsString() + detailer.Result("\r\n-- -\r\nDetails: "));
            return list[index];
        }

        /// <summary>
        /// Gives better error messages when reading value from directory if key does not exist
        /// 
        /// Note how <see cref="GetValue2"/> is more preferable than <see cref="GetValue"/> due to restrictions on <typeparamref name="TKey"/>
        /// (explained in <see cref="KeysAsString2{TKey, TValue}(Dictionary{TKey, TValue})"/>)
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
        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<string> detailer = null) {
            if (dictionary == null) throw new NullReferenceException(nameof(dictionary) + detailer.Result("\r\nDetails: "));
            return dictionary.TryGetValue(key, out var retval) ? retval : throw new KeyNotFoundException(Util.BreakpointEnabler + "Key '" + key.ToString() + "' not found in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString() + detailer.Result("\r\n---\r\nDetails: "));
        }
        /// <summary>
        /// Gives better error messages when reading value from directory if key does not exist
        /// 
        /// Note how <see cref="GetValue2"/> is more preferable than <see cref="GetValue"/> due to restrictions on <typeparamref name="TKey"/>
        /// (explained in <see cref="KeysAsString2{TKey, TValue}(ConcurrentDictionary{TKey, TValue})"/>)
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
        public static TValue GetValue<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<string> detailer = null) {
            if (dictionary == null) throw new NullReferenceException(nameof(dictionary) + detailer.Result("\r\nDetails: "));
            return dictionary.TryGetValue(key, out var retval) ? retval : throw new KeyNotFoundException(Util.BreakpointEnabler + "Key '" + key.ToString() + "' not found in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString() + detailer.Result("\r\n---\r\nDetails: "));
        }

        /// <summary>
        /// Gives better error messages when reading value from directory if key does not exist
        /// 
        /// Note how <see cref="GetValue2"/> is more preferable than <see cref="GetValue"/> due to restrictions on <typeparamref name="TKey"/> 
        /// (explained in <see cref="KeysAsString2{TKey, TValue}(Dictionary{TKey, TValue})"/>)
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
        public static TValue GetValue2<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<string> detailer = null) where TKey : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (dictionary == null) throw new NullReferenceException(nameof(dictionary) + detailer.Result("\r\nDetails: "));
            return dictionary.TryGetValue(key, out var retval) ? retval : throw new KeyNotFoundException("Key '" + key.ToString() + "' not found in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString2() + detailer.Result("\r\n---\r\nDetails: "));
        }

        /// <summary>
        /// Gives better error messages when reading value from directory if key does not exist
        /// 
        /// Note how <see cref="GetValue2"/> is more preferable than <see cref="GetValue"/> due to restrictions on <typeparamref name="TKey"/>
        /// (explained in <see cref="KeysAsString2{TKey, TValue}(ConcurrentDictionary{TKey, TValue})"/>)
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
        public static TValue GetValue2<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<string> detailer = null) where TKey : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (dictionary == null) throw new NullReferenceException(nameof(dictionary) + detailer.Result("\r\nDetails: "));
            return dictionary.TryGetValue(key, out var retval) ? retval : throw new KeyNotFoundException("Key '" + key.ToString() + "' not found in dictionary. Dictionary.Count: " + dictionary.Count + " " + dictionary.KeysAsString2() + detailer.Result("\r\n---\r\nDetails: "));
        }

        /// <summary>
        /// Convenience method making <see cref="ConcurrentDictionary{TKey, TValue}"/> behave more like <see cref="Dictionary{TKey, TValue}"/>. 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Add<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue value) {
            if (!dictionary.TryAdd(key, value)) throw new KeyAlreadyExistsException("Key '" + key + "' already exists. Unable to add value " + value.ToString());
        }

        /// <summary>
        /// Convenience method making <see cref="ConcurrentDictionary{TKey, TValue}"/> behave more like <see cref="Dictionary{TKey, TValue}"/>. 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        public static void Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key) {
            if (!dictionary.TryRemove(key, out _)) throw new KeyNotFoundException("Key '" + key + "' does not exist. Unable to remove value");
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

        /// <summary>
        /// TODO: Purist will most probably object to this method.
        /// TODO: Analyze any consequences more throughly
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<T> ToList<T>(this System.Collections.IList list) {
            var retval = new List<T>();
            if (typeof(T).Equals(typeof(string))) {
                foreach (var element in list) {
                    // TODO: Make better. See exceptions elsewhere in code for double and DateTime.
                    retval.Add((T)(object)element.ToString());
                }
            } else {
                foreach (var element in list) {
                    InvalidTypeException.AssertAssignable(element.GetType(), typeof(T), null);
                    retval.Add((T)element);
                }
            }
            return retval;
        }

        [ClassMember(Description =
            "Alernative to IEnumerable.ToDictionary. " +
            "Gives better exception for duplicate keys (will explain WHICH key is a duplicate).")]
        public static Dictionary<TKey, TElement> ToDictionary2<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<string> detailer = null) {
            var retval = new Dictionary<TKey, TElement>();
            source.ForEach(e => {
                retval.AddValue(keySelector(e), elementSelector(e), () => "Element " + e.ToString() + detailer.Result("\r\nDetails: "));
            });
            return retval;
        }

        private static ConcurrentDictionary<Type, List<(Type, PropertyKey)>> _typesWhereIsForeignKeyCache = new ConcurrentDictionary<Type, List<(Type, PropertyKey)>>();
        public static List<(Type type, PropertyKey key)> GetTypesWhereIsForeignKey(this Type type) => _typesWhereIsForeignKeyCache.GetOrAdd(type, t =>
            PropertyKeyMapper.AllCoreP.Where(k => k.Key.A.ForeignKeyOf == t).SelectMany(k => k.Key.A.Parents == null ? new List<(Type, PropertyKey)>() : k.Key.A.Parents.Select(p => (p, k)).ToList()).ToList());

        public static EntityTypeCategory ToEntityTypeCategory(this Type type) {
            if (typeof(Agent).IsAssignableFrom(type)) return EntityTypeCategory.Agent;
            if (typeof(APIDataObject).IsAssignableFrom(type)) return EntityTypeCategory.APIDataObject;
            if (typeof(ApplicationPart).IsAssignableFrom(type)) return EntityTypeCategory.ApplicationPart;
            if (typeof(BaseEntity).IsAssignableFrom(type)) return EntityTypeCategory.BaseEntity;
            throw new InvalidTypeException(type, "Not of type " + typeof(BaseEntity));
        }

        private static ConcurrentDictionary<Type, string> _toStringShortCache = new ConcurrentDictionary<Type, string>();
        /// <summary>
        /// See <see cref="CoreP.EntityTypeShort"/>
        /// See also <see cref="ToStringDB(Type)"/> for a string representation compatible with <see cref="Util.TryGetTypeFromString"/>. 
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
                Replace("`2+", "+").  // inner classes 
                Replace("`3+", "+").
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
        /// See <see cref="CoreP.EntityTypeVeryShort"/>
        /// Returns same as <see cref="ToStringShort"/> but without any generics information at all. 
        /// See also <see cref="ToStringDB(Type)"/> for a string representation compatible with <see cref="Util.TryGetTypeFromString"/>. 
        /// See also <see cref="ToStringShort(Type)"/>. 
        /// 
        /// See also invers method <see cref="APIMethod.GetTypeFromVeryShortString"/>
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
        /// Now how types in <see cref="APIMethod.AllEntityTypes"/> are stored in a short-hand form.
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
            if (APIMethod.AllEntityTypes.Contains(t)) return t.ToStringVeryShort(); /// Now how types in <see cref="APIMethod.AllEntityTypes"/> are stored in a short-hand form.
            var retval = t.ToString() + "," + t.Assembly.GetName().Name;
            if (!t.IsGenericType) return ToStringShort(t) + " : " + retval;
            var arguments = t.GetGenericArguments();
            if (arguments.Length != 1) throw new InvalidTypeException(type, nameof(t.GetGenericArguments) + ".Length != 1. Handling if this is just not implemented yet.");
            if (retval.Split(']').Length != 2) throw new InvalidTypeException(type, retval + " does not contain exactly one ]");
            return ToStringShort(t) + " : " + retval.Replace("]", "," + arguments[0].Assembly.GetName().Name + "]");
        });


        public static string ToString(this DateTime dateTime, DateTimeFormat resolution) {
            switch (resolution) {
                case DateTimeFormat.None:
                case DateTimeFormat.DateHourMinSecMs: return dateTime.ToString(Util.Configuration.C.DateAndHourMinSecMsFormat);
                case DateTimeFormat.DateHourMinSec: return dateTime.ToString(Util.Configuration.C.DateAndHourMinSecFormat);
                case DateTimeFormat.DateHourMin: return dateTime.ToString(Util.Configuration.C.DateAndHourMinFormat);
                case DateTimeFormat.DateOnly: return dateTime.ToString(Util.Configuration.C.DateOnlyFormat);
                default: throw new InvalidEnumException(resolution);
            }
        }

        public static string ToString(this long number, NumberFormat resolution) {
            switch (resolution) {
                case NumberFormat.None:
                case NumberFormat.Id: return number.ToString(Util.Configuration.C.NumberIdFormat);
                case NumberFormat.Integer: return number.ToString(Util.Configuration.C.NumberIntegerFormat);
                case NumberFormat.Decimal: return number.ToString(Util.Configuration.C.NumberDecimalFormat); // A little strange but plausible
                default: throw new InvalidEnumException(resolution);
            }
        }

        /// <summary>
        /// Convenience method for presenting decimal numbers. Facilitates easy use of decimal point consequently throughout the application
        /// TODO: Reduce use of this now that <see cref="PropertyKeyAttribute.NumberFormat"/> has been introduced 
        /// TODO: (use <see cref="ToString(double, NumberFormat)"/> more instead.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string ToString2(this double number) => number.ToString("0.00").Replace(",", ".");

        public static string ToString(this double number, NumberFormat resolution) {
            switch (resolution) {
                case NumberFormat.None:
                case NumberFormat.Decimal: return number.ToString(Util.Configuration.C.NumberDecimalFormat);
                case NumberFormat.Integer: return number.ToString(Util.Configuration.C.NumberIntegerFormat); // A little strange but plausible
                case NumberFormat.Id: return number.ToString(Util.Configuration.C.NumberIdFormat); // Quite strange / irrelevant
                default: throw new InvalidEnumException(resolution);
            }
        }

        /// <summary>
        /// Returns empty string if function is null
        /// </summary>
        /// <param name="function">May be null</param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static string Result(this Func<string> function, string header) {
            if (function == null) return "";
            return header + function();
        }

        private static ConcurrentDictionary<Type, List<APIMethod>> _entityOperationUrlsCache = new ConcurrentDictionary<Type, List<APIMethod>>();
        [ClassMember(Description = 
            "Returns relevant methods containing -" + nameof(APIMethodP.EntityOperationUrl) + "- for the given type.\r\n")]
        public static List<APIMethod> GetEntityOperationUrlsForType(this Type type) => _entityOperationUrlsCache.GetOrAdd(type, t => APIMethod.AllMethods.Where(m =>
            m.EntityType != null &&
            m.EntityType.IsAssignableFrom(t) &&
            m.PV<List<Uri>>(APIMethodP.EntityOperationUrl.A()).Count > 0
        ).ToList());

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
                /// (see also <see cref="BaseDatabase.TryGetEntities"/> which has the main responsibility for <see cref="AccessLocation.Relation"/>). 
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
        /// 
        /// NOTE: Methods like <see cref="GetChildPropertiesByPriority"/> do not take into account access rights.
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
        /// Has practical use when calling <see cref="BaseDatabase.CreateEntity"/>
        /// 
        /// Now how returns <see cref="PropertyKey.PropertyKeyWithIndex"/> from <see cref="GetChildProperties"/>.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Dictionary<CoreP, PropertyKeyWithIndex> GetObligatoryChildProperties(this Type type) => _obligatoryChildPropertiesCache.GetOrAdd(type, t => GetChildProperties(type).Where(e => e.Value.Key.A.IsObligatory).ToDictionary(e => e.Key, e => e.Value.Key.A.IsMany ? new PropertyKeyWithIndex(e.Value.Key, 1) : e.Value.PropertyKeyWithIndex));

        private static ConcurrentDictionary<
            string,  // Key is type + "_" + priorityOrder
            List<PropertyKey>> _childPropertiesByPriorityCache = new ConcurrentDictionary<string, List<PropertyKey>>();
        /// <summary>
        /// See <see cref="GetChildProperties"/> for details.
        /// 
        /// Returns list sorted by <see cref="PropertyKeyAttribute.PriorityOrder"/> (and alphabetic within that again)
        /// 
        /// Useful for listing out objects for instance, with important priorities 
        /// to the left in <see cref="BaseEntity.ToHTMLTableRow"/> and 
        /// at the top for <see cref="BaseEntity.ToHTMLDetailed"/> (although not used as of Sep 2017 in that method).
        /// 
        /// Restricts to <paramref name="withinThisPriority"/>. 
        /// NOTE: Does not take into account any access rights. TODO: Improve on this situation (see methods like <see cref="GetChildPropertiesForAccessLevel"/>)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="withinThisPriority">
        /// All properties with <see cref="PropertyKeyAttribute.PriorityOrder"/> less than or equal to this value will be returned. 
        /// 
        /// Since result is cached, the values used for this parameter should have a limited range.
        /// Use <see cref="PriorityOrder.Everything"/> (which corresponds to <see cref="int.MaxValue"/>) if you want absolutely all properties 
        /// (note that the <see cref="PriorityOrder"/>-enum is actually an int, so any int-value can be stored)
        /// </param>
        /// <returns></returns>
        public static List<PropertyKey> GetChildPropertiesByPriority(this Type type, PriorityOrder withinThisPriority) => _childPropertiesByPriorityCache.GetOrAdd(type + "_" + withinThisPriority, key => {
            var retval = GetChildProperties(type).Where(e => e.Value.Key.A.PriorityOrder <= withinThisPriority).Select(e => e.Value).ToList();
            retval.Sort(new Comparison<PropertyKey>((key1, key2) => {
                var o = key1.Key.A.PriorityOrder.CompareTo(key2.Key.A.PriorityOrder);
                if (o != 0) return o;
                return key1.Key.PToString.CompareTo(key2.Key.PToString); // Use alphabetic ordering.
            }));
            return retval;
        });

        private static ConcurrentDictionary<Type, Dictionary<CoreP, PropertyKey>> _childPropertiesCache = new ConcurrentDictionary<Type, Dictionary<CoreP, PropertyKey>>();
        /// <summary>
        /// Returns all <see cref="CoreP"/> for which <paramref name="type"/> is in <see cref="PropertyKeyAttribute.Parents"/>. 
        /// Note how result is cached.
        /// 
        /// TODO: Add reset of cache (because of <see cref="PropertyKeyMapper.GetA(string, BaseDatabase)"/> which will add new mappings after application startup)
        /// TODO: OR EVEN BETTER, MOVE INTO <see cref="PropertyKeyMapper"/> INSTEAD
        /// 
        /// TODO: As of Apr 2017 it looks like <see cref="PropertyKeyMapper.GetA(string, BaseDatabase)"/> is not going to be used after all
        /// TODO: (corresponding functionality has been put into <see cref="PostgreSQLDatabase"/>.ReadOneProperty instead.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Dictionary<CoreP, PropertyKey> GetChildProperties(this Type type) {
            PropertyKeyMapper.AssertMapEnumFinalizeHasCompleted(); // This assert is done in order to avoid invalid cache issues if called during startup.
            return _childPropertiesCache.GetOrAdd(type, t => PropertyKeyMapper.AllCoreP.Where(key => key.Key.HasParentOfType(type)).ToDictionary(key => key.Key.CoreP, key => key));
        }

        public static PropertyKey GetExternalPrimaryKey(this Type type) => TryGetExternalPrimaryKey(type, out var retval) ? retval : throw new ExternalPrimaryKeyNotFoundException(
            nameof(PropertyKeyAttribute.ExternalForeignKeyOf) + " not found for " + type + ".\r\n" +
            "Possible resolution: Ensure that -" + nameof(PropertyKeyAttribute) + "-.- " + nameof(PropertyKeyAttribute.ExternalPrimaryKeyOf) + "- has been defined for one of the properties belonging to " + type.ToStringShort() + "\r\n" +
            "The current properties defined for " + type.ToStringShort() + " are: " + string.Join(", ", GetChildProperties(type)) + "\r\n" +
            (GetChildProperties(type).Count > 0 ? "" : ("General hint: Remember mapping of " + nameof(EnumType.PropertyKey) + "-enums in your Startup.cs (refer to code block which ends with " + nameof(PropertyKeyMapper.MapEnumFinalize) + ")")));

        private static ConcurrentDictionary<Type, PropertyKey> _externalPrimaryKeysCache = new ConcurrentDictionary<Type, PropertyKey>();
        public static bool TryGetExternalPrimaryKey(this Type type, out PropertyKey propertyKey) => null != (propertyKey = _externalPrimaryKeysCache.GetOrAdd(type, t => {
            var candidates = t.GetChildProperties().Values.Where(k => k.Key.A.ExternalPrimaryKeyOf != null).ToList();
            switch (candidates.Count) {
                case 0: return null;
                case 1: return candidates.First();
                default:
                    throw new ExternalPrimaryKeyNotFoundException(
                       "Multiple (" + candidates.Count + ") " + nameof(PropertyKeyAttribute.ExternalForeignKeyOf) + " found for " + type + ".\r\n" +
                       "Possible resolution: Ensure that there is exact one -" + nameof(PropertyKeyAttribute) + "-.- " + nameof(PropertyKeyAttribute.ExternalPrimaryKeyOf) + "- defined for one of the properties belonging to " + type.ToStringShort() + "\r\n" +
                       "Currenctly the following " + candidates.Count + " are defined: " + string.Join(", ", candidates.Select(c => c.Key.PToString)));
            }
        }));

        public class ExternalPrimaryKeyNotFoundException : ApplicationException {
            public ExternalPrimaryKeyNotFoundException(string message) : base(message) { }
        }

        private static ConcurrentDictionary<Type, ClassAttribute> _classAttributeCache = new ConcurrentDictionary<Type, ClassAttribute>();
        /// <summary>
        /// Note use of caching. 
        /// See <see cref="ClassAttribute.GetAttribute"/> for documentation. 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ClassAttribute GetClassAttribute(this Type type) => _classAttributeCache.GetOrAdd(type, t => ClassAttribute.GetAttribute(t));

        private static ConcurrentDictionary<string, ClassMemberAttribute> _classMemberAttributeCache = new ConcurrentDictionary<string, ClassMemberAttribute>();
        /// <summary>
        /// Note use of caching. 
        /// See <see cref="ClassMemberAttribute.GetAttribute"/> for documentation. 
        /// NOTE: Use with caution.
        /// NOTE: Will not work for overloaded methods. 
        /// NOTE: Overload <see cref="GetClassMemberAttribute(System.Reflection.MemberInfo)"/> is preferred. 
        /// 
        /// TODO: This is most probably not needed. It is used in order to get to a <see cref="ClassMemberAttribute"/> but that one again
        /// TODO: is most probably only used in order to get to a <see cref="System.Reflection.MemberInfo"/> object (<see cref="ClassMemberAttribute.MemberInfo"/>)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ClassMemberAttribute GetClassMemberAttribute(this Type type, string member) => _classMemberAttributeCache.GetOrAdd(type + "." + member, k => ClassMemberAttribute.GetAttribute(type, member));

        /// <summary>
        /// Searches for the <see cref="System.Diagnostics.StackFrame"/> corresponding to <paramref name="preferredMethodName"/>
        /// 
        /// Useful when caller is an anonymous method / lambda expression, which would give a rather unwieldy <see cref="Id.IdString"/>
        /// 
        /// See http://stackoverflow.com/questions/171970/how-can-i-find-the-method-that-called-the-current-method
        /// </summary>
        /// <param name="memberInfo">
        /// Note how this parameter is actually not very relevant 
        /// (and there is no reason for this method to actually be an extension method)
        /// </param>
        /// <param name="preferredMethodName"></param>
        /// <returns></returns>
        public static ClassMemberAttribute GetClassMemberAttribute(this System.Reflection.MemberInfo memberInfo, string preferredMethodName) {
            if (memberInfo.Name.Equals(preferredMethodName)) return memberInfo.GetClassMemberAttribute(); // Not very probable
            System.Reflection.MethodBase method = null;
            var i = 0; while (method == null || !method.Name.Equals(preferredMethodName)) {
                if (i > 3) throw new InvalidCountException(nameof(i) + ": " + i + ". Did not find method named " + preferredMethodName + " on current stack");
                method = new System.Diagnostics.StackFrame(i++).GetMethod();
            }
            return method.GetClassMemberAttribute();
        }

        /// <summary>
        /// Preferred overload
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        public static ClassMemberAttribute GetClassMemberAttribute(this System.Reflection.MemberInfo memberInfo) => GetClassMemberAttributeNonStrict(memberInfo) ?? throw new NullReferenceException(System.Reflection.MethodBase.GetCurrentMethod().Name + ". Check for " + nameof(ApplicationPart.GetFromDatabaseInProgress) + ". Consider calling " + nameof(GetClassMemberAttributeNonStrict) + " instead");

        public static ClassMemberAttribute GetClassMemberAttributeNonStrict(this System.Reflection.MemberInfo memberInfo) {
            if (ApplicationPart.GetFromDatabaseInProgress) {
                /// This typical happens when called from <see cref="ReadAllPropertyValuesAndSetNoLongerCurrentForDuplicates"/> because that one wants to
                /// <see cref="PropertyOperation.SetInvalid"/> some <see cref="Property"/> for a <see cref="ClassMember"/>.
                return null;
            }
            /// Careful with cache key here. <see cref="System.Reflection.MemberInfo.Name"/> is not sufficient because of overloads.
            return _classMemberAttributeCache.GetOrAdd(memberInfo.DeclaringType + "." + memberInfo.ToString(), k => ClassMemberAttribute.GetAttribute(memberInfo));
        }

        private static ConcurrentDictionary<Type, EnumAttribute> _enumAttributeCache = new ConcurrentDictionary<Type, EnumAttribute>();
        /// <summary>
        /// Note use of caching. 
        /// See <see cref="EnumAttribute.GetAttribute"/> for documentation. 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static EnumAttribute GetEnumAttribute(this Type type) => _enumAttributeCache.GetOrAdd(type, t => EnumAttribute.GetAttribute(t));

        private static ConcurrentDictionary<Type, ConcurrentDictionary<string, EnumValueAttribute>> _enumValueAttributeCache = new ConcurrentDictionary<Type, ConcurrentDictionary<string, EnumValueAttribute>>();
        /// <summary>
        /// Note how sub class <see cref="PropertyKeyAttribute"/> of <see cref="EnumValueAttribute"/> 
        /// is returned as appropriate (if <see cref="GetEnumAttribute"/> returns <see cref="EnumType.PropertyKey"/>)
        /// </summary>
        /// <param name="_enum"></param>
        /// <returns></returns>
        public static EnumValueAttribute GetEnumValueAttribute(this object _enum) {
            var type = _enum.GetType();
            NotOfTypeEnumException.AssertEnum(type);
            return _enumValueAttributeCache.
                GetOrAdd(type, dummy => new ConcurrentDictionary<string, EnumValueAttribute>()).
                GetOrAdd(_enum.ToString(), dummy => EnumValueAttribute.GetAttribute(_enum));
        }

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
        /// Makes "safe" attempt of turning an entity id into <see cref="BaseEntity.IdFriendly"/> through <see cref="Util.EntityCache"/>
        /// 
        /// Note possible security consequences of this method. Use sparingly or not at all if in doubt.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string AsEntityName(this long id) => id <= 0 ? "[NOT SET]" : (InMemoryCache.EntityCache.TryGetValue(id, out var e) ? e.IdFriendly : id.ToString());

        public static List<string> Split(this string _string, string separator) => _string.Split(new string[] { separator }, StringSplitOptions.None).ToList();

        public static string HTMLEncode(this string _string, int? maxLength = null) {
            if (maxLength != null && _string.Length > maxLength) {
                return System.Net.WebUtility.HtmlEncode(_string.Substring(0, (int)maxLength) + "...").
                    HTMLEncloseWithinTooltip(_string);
            }
            return System.Net.WebUtility.HtmlEncode(_string);
        }
        /// <summary>
        /// 1) Adds {br} to every new line
        /// 2) Turns into hyperlinks if <paramref name="_string"/>.StartsWith "http?//"
        /// 
        /// TODO: Add other enhancements
        /// </summary>
        /// <param name="_string"></param>
        /// <param name="api"></param>
        /// <param name="maxLength">
        /// Maximum length of <paramref name="_string"/> to show. The rest will be handled by <see cref="HTMLEncloseWithinTooltip"/>. 
        /// Note that applies to each separate line of <paramref name="_string"/>, not the whole.
        /// </param>
        /// <returns></returns>
        public static string HTMLEncodeAndEnrich(this string _string, APICommandCreator api, int? maxLength = null) {
            if (_string.StartsWith("http://") || _string.StartsWith("https://")) {
                var b = Util.Configuration.C.BaseUrl.ToString();
                return string.Join("<br>\r\n", _string.Split("\r\n").Select(s => "<a href=\"" + s + (
                    (
                        s.StartsWith(b) && // Added restriction to only internal links at 16 Nov 2018.
                        api.ResponseFormat == ResponseFormat.HTML && !s.EndsWith(Util.Configuration.C.HTMLPostfixIndicator)
                    ) ?
                    Util.Configuration.C.HTMLPostfixIndicator :  // For internal links (within AgoRapide-site), keep result of clicking link in HTML-format also in HTML-format.
                    "" /// For external links or when result is not <see cref="ResponseFormat.HTML"/> (like <see cref="ResponseFormat.JSON"/> or <see cref="ResponseFormat.CSV"/>), do not modify link
                ) +
                "\">" + s.HTMLEncode(maxLength) + "</a>"));
            }
            return _string.HTMLEncode(maxLength).Replace("\r\n", "<br>\r\n");
        }

        /// <summary>
        /// Encloses <paramref name="html"/> within a HTML span with title <paramref name="tooltip"/>
        /// 
        /// TODO: Consider switching parameter ordering or renaming method. 
        /// </summary>
        /// <param name="html">Already encoded as HTML</param>
        /// <param name="tooltip">Not to be encoded as HTML. May be null or empty in which case only <paramref name="html"/> will be returned</param>
        /// <returns></returns>
        public static string HTMLEncloseWithinTooltip(this string html, string tooltip, bool excludeHintThatTooltipExists = false) => string.IsNullOrEmpty(tooltip) ? html : "<span title=\"" + tooltip.HTMLEncode() + "\">" + html + (excludeHintThatTooltipExists ? "" : " (+)") + "</span>";

        /// <summary>
        /// Encloses <paramref name="html"/> within toogle-click to show / hide. 
        /// 
        /// TODO: Consider switching parameter ordering or renaming method. 
        /// </summary>
        /// <param name="title">Not to be encoded as HTML</param>
        /// <param name="html">Already encoded as HTML</param>
        /// <returns></returns>
        public static string HTMLEncloseWithInVisibilityToggle(this string title, string html) {
            var id = Util.GetNextId();
            return
                "<div id=\"header_" + id + "\" name=\"header_" + id + "\" onclick=\"try { " +
                    "$('#inner_" + id + "').toggle(); " +
                    "} catch (err) { console.log(err.toString()); } return false;\">" +
                    title.HTMLEncode() + " ...<br>" +
                "</div>\r\n" + /// First div-tag must be terminated here, because <param name="html"/> may contain links which would be deactived if the onclick-event was valid here also
                "<div id=\"inner_" + id + "\" name=\"inner_" + id + "\" style=\"display:none\">\r\n" +
                    html + "\r\n" +
                "</div>\r\n";
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="key"></param>
        /// <param name="detailer">May be null</param>
        /// <returns></returns>
        public static Property ToIsManyParent<T>(this List<T> list, BaseEntity parent, PropertyKey key, Func<string> detailer) => Util.ConvertListToIsManyParent(parent, key, list, detailer);

        public static T Second<T>(this List<T> collection) => collection.ElementAt(1);
        public static T SecondOrDefault<T>(this List<T> collection) => collection.ElementAtOrDefault(1);

        public static T Third<T>(this List<T> collection) => collection.ElementAt(2);
        public static T ThirdOrDefault<T>(this List<T> collection) => collection.ElementAtOrDefault(2);

        public static T Fourth<T>(this List<T> collection) => collection.ElementAt(3);
        public static T FourthOrDefault<T>(this List<T> collection) => collection.ElementAtOrDefault(3);

        public static T Fifth<T>(this List<T> collection) => collection.ElementAt(4);
        public static T FifthOrDefault<T>(this List<T> collection) => collection.ElementAtOrDefault(4);
    }
}