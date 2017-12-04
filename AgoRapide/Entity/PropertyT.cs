// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;
using AgoRapide.Database; // Used by XML-links

namespace AgoRapide {

    /// <summary>
    /// See <see cref="Property"/> for overview and detailed documentation about properties. 
    /// 
    /// Note that <typeparamref name="T"/> does not necessarily have to correspond to <see cref="PropertyKeyAttribute.Type"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Class(Description =
        "Generic sub-class of -" + nameof(Property) + "- which is meant to hide some complexity " +
        "and ease the understanding of the super class -" + nameof(Property) + "-.")]
    public class PropertyT<T> : Property {

        /// <summary>
        /// Do not use directly. Use through <see cref="ParseResult.Create{T}"/> and <see cref="BaseEntity.AddProperty{T}"/>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public PropertyT(PropertyKeyWithIndex key, T value) : this(key, value, null, null) { }

        /// <summary>
        /// Preferred overload if <paramref name="strValue"/> is known by caller. 
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">
        /// Note that <see cref="T"/> equal to <see cref="object"/> is tolerated, and in most cases quite OK since the generic
        /// subclass <see cref="PropertyT{T}"/> is little used in AgoRapide anyway.
        /// 
        /// Note that it is possible to use <see cref="string"/> as generic parameter here even when
        /// <see cref="PropertyKeyAttribute.Type"/> is something different that <see cref="string"/>. 
        /// </param>
        /// <param name="strValue">
        /// May be null. If not given then corresponding conversion of <paramref name="value"/> to string will be used. 
        /// </param>
        /// <param name="valueAttribute">
        /// May be null. See <see cref="Property.ValueA"/> for how it then will be found. 
        /// </param>
        public PropertyT(PropertyKeyWithIndex key, T value, string strValue, BaseAttribute valueAttribute) : base(dummy: null) {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _value = (object)value ?? throw new ArgumentNullException(nameof(value));
            _genericValue = value;

            if (typeof(string).Equals(typeof(T))) {
                /// Do not bother with type checking now against <see cref="PropertyKeyAttribute.Type"/>
                /// But note that <see cref="Property.TryGetV{T}"/> will fail at a later stage if called with something other than string. 
                /// Note that this is a pragmatic decision. 
                /// TODO: Consider tightening up
                /// TODO: Typical example for when needed now is code like this:
                /// TODO:    request.Result.AddProperty(CoreP.SuggestedUrl.A(), string.Join("\r\n", cmds.Select(cmd => request.CreateAPIUrl(cmd))));
                /// TODO: List[Uri] would be correct here
            } else {
                /// We can not do this:
                ///   InvalidTypeException.AssertAssignable(typeof(T), key.Key.A.Type, () => nameof(PropertyKeyAttribute) + "." + nameof(PropertyKeyAttribute.Type) + " for " + key.Key.PExplained + " !IsAssignableFrom " + typeof(T));
                /// because often we are called with T = object like from here (See <see cref="PropertyKeyAttributeEnriched"/>):
                ///   if (Util.EnumTryParse(A.Type, value, out var temp)) return ParseResult.Create(this, temp);
                /// Instead we must do like this, using the actual type that we got:
                InvalidTypeException.AssertAssignable(value.GetType(), key.Key.A.Type, () => nameof(PropertyKeyAttribute) + "." + nameof(PropertyKeyAttribute.Type) + " for " + key.Key.A.EnumValueExplained + " !IsAssignableFrom " + value.GetType());
            }
            _stringValue = strValue ?? new Func<string>(() => {
                /// This is typically the case when called from <see cref="ParseResult.Create"/>        
                /// Note how <see cref="ParseResult.Create{T}"/> does not give us <paramref name="strValue"/> even though it would have
                /// been easy for it again to have that as an incoming parameter. But that string value would not necessarily be
                /// the same as <paramref name="value"/>.ToString() (the actual parser being used may accept very different input)

                // switch (value) // Wondering why we can not switch on value directly? See http://stackoverflow.com/questions/41436399/expression-of-type-t-cannot-be-handled-by-a-pattern-of-type-x
                var objValue = (object)value;
                switch (objValue) {
                    case long v: return v.ToString(key.Key.A.NumberFormat);
                    // case double v: return v.ToString2();
                    case double v: return v.ToString(key.Key.A.NumberFormat);
                    case DateTime v: return v.ToString(key.Key.A.DateTimeFormat); /// Correction 22 Sep 2017 (instead of always using <see cref="DateTimeFormat.DateHourMin"/>)
                    case TimeSpan v: return v.TotalHours < 24 ? v.ToString(@"hh\:mm\:ss") : v.ToString(@"d\.hh\:mm\:ss"); /// Note corresponding code in <see cref="PropertyT{T}.PropertyT"/> and <see cref="PropertyKeyAttributeEnriched.Initialize"/>
                    case Type v: return v.ToStringDB(); // Added 12 Jun 2017
                    default: return objValue.ToString();
                }
            })();
        }

        private T _genericValue;
    }
}