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
    /// See <see cref="PropertyT{T}.PropertyT"/> for details.
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
                ///   InvalidTypeException.AssertAssignable(typeof(T), key.Key.A.Type, () => nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " for " + key.Key.PExplained + " !IsAssignableFrom " + typeof(T));
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
                    case double dblValue: return dblValue.ToString2();
                    case DateTime dtmValue: return dtmValue.ToString(DateTimeFormat.DateHourMin);
                    case Type type: return type.ToStringDB(); // Added 12 Jun 2017
                    default: return objValue.ToString();
                }
            })();
        }

        /// <summary>
        /// TODO: Might this replace all the others XXXValue-properties?
        /// 
        /// TODO: Change name into objValue maybe?
        /// </summary>
        private T _genericValue;
        ///// <summary>
        ///// Note how this throws an Exception if <see cref="_ADotTypeValue"/> is not set. 
        ///// Property should be used with caution. Usually used from <see cref="Parameters"/> when ... ????????? 
        ///// TODO: DOCUMENT BETTER! USE BETTER!
        ///// 
        ///// TODO: Make much better. Try to avoid if-else-if-else-if ... below.
        ///// TODO: Think through all this much better!
        ///// </summary>
        //public object ADotTypeValue() => TryGetADotTypeValue(out var retval) ? retval : throw new NullReferenceException(nameof(_ADotTypeValue) + ": This property is usually set from this constructor: public Property(TProperty keyT, object objValue). Details: " + ToString());
        //public bool TryGetADotTypeValue(out object aDotTypeValue) {
        //    var type = Key.Key.A.Type;
        //    if (type == null) {
        //        if (_ADotTypeValue == null) {
        //            // TODO: This is NOT preferred! We are assuming string as default type
        //            aDotTypeValue = V<string>(); // This will also set _ADotTypeValue for next call
        //        } else {
        //            aDotTypeValue = _ADotTypeValue; // TODO: This is NOT preferred! We are just accepting whatever we already have. Clean up this!
        //        }
        //    } else if (_ADotTypeValue != null && _ADotTypeValue.GetType().Equals(type)) {
        //        // This is quite OK
        //        aDotTypeValue = _ADotTypeValue;
        //    } else if (type.Equals(typeof(long))) {
        //        aDotTypeValue = V<long>();
        //    } else if (type.Equals(typeof(double))) {
        //        aDotTypeValue = V<double>();
        //    } else if (type.Equals(typeof(bool))) {
        //        aDotTypeValue = V<bool>();
        //    } else if (type.Equals(typeof(DateTime))) {
        //        aDotTypeValue = V<DateTime>();
        //    } else if (type.Equals(typeof(string))) {
        //        aDotTypeValue = V<string>();
        //    } else if (type.Equals(typeof(Type))) {
        //        aDotTypeValue = V<Type>();
        //    } else if (type.Equals(typeof(Uri))) {
        //        aDotTypeValue = V<Uri>();
        //    } else if (type.IsEnum) {
        //        /// TODO: We should be able to call <see cref="V{T}"/> here also...
        //        /// Do not use Util.EnumParse, we want a more detailed exception message
        //        /// aDotTypeValue = Util.EnumParse(KeyA.A.Type, V<string>());
        //        /// Or rather, do not do this either:
        //        // aDotTypeValue = Util.EnumTryParse(KeyA.A.Type, V<string>(), out var temp) ? temp : throw new InvalidPropertyException("Unable to parse '" + V<string>() + "' as " + KeyA.A.Type + ". Details: " + ToString());
        //        /// But this:
        //        aDotTypeValue = Util.EnumTryParse(type, V<string>(), out var temp) ? temp : null; // Just give up if fails
        //    } else if (typeof(ITypeDescriber).IsAssignableFrom(type)) {
        //        aDotTypeValue = Key.Key.TryValidateAndParse(V<string>(), out var temp) ? temp.ObjResult : null;
        //    } else {
        //        throw new InvalidTypeException(type, "Not implemented. Details: " + ToString());
        //    }
        //    return aDotTypeValue != null;
        //}

        ///// <summary>
        ///// The general value.
        ///// Populated from one of lng, dbl, dtm, geo, strValue by Initialize
        ///// 
        ///// TODO: THIS IS CANDIDATE FOR REMOVAL. PROBABLY NOT NEEDED.
        ///// 
        ///// TODO: Rename into DebugValue. Set it from the future planned generic subclass of <see cref="Property"/>
        ///// TODO: (Mar 2017 release of Visual Studio crashes when attempting to rename this property)
        ///// </summary>
        //public string Value { get; private set; }

        // -----------------------------------------------------
        // Fields as stored in database
        // TODO: CONSIDER REMOVING ALL THESE!
        // -----------------------------------------------------

        ///// <summary>
        ///// <see cref="DBField.lngv"/>
        ///// </summary>
        //private long? LngValue;
        ///// <summary>
        ///// <see cref="DBField.dblv"/>
        ///// </summary>
        //private double? DblValue;
        ///// <summary>
        ///// <see cref="DBField.blnv"/>
        ///// </summary>
        //private bool? BlnValue;
        ///// <summary>
        ///// <see cref="DBField.dtmv"/>
        ///// </summary>
        //private DateTime? DtmValue;
        ///// <summary>
        ///// <see cref="DBField.geov"/>
        ///// </summary>
        //private string GeoValue;
        ///// <summary>
        ///// <see cref="DBField.strv"/>
        ///// </summary>
        //private string StrValue;

        // -----------------------------------------------------
    }
}