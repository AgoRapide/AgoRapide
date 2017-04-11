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
    /// You should normally not use constructors here (like <see cref="Create{T}(TProperty, T)"/>)  
    /// but instead rely on methods like <see cref="IDatabase.TryGetPropertyById"/> and <see cref="BaseEntity.AddProperty"/>
    /// in order to ensure correct population of fields like <see cref="ParentId"/> and <see cref="Parent"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [AgoRapide(Description =
        "Generic sub-class of -" + nameof(Property) + "- which is meant to hide some complexity " +
        "and ease the understanding of the super class -" + nameof(Property) + "-.")]
    public class PropertyT<T> : Property {

        ///// <summary>
        ///// Now how <see cref="BaseEntity.AddProperty)"/> is preferred when adding property to entity
        ///// 
        ///// Note that does NOT call <see cref="Initialize"/> 
        ///// 
        ///// (since further setting av properties like <see cref="ParentId"/> is most probably needed first)
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="key"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //public static PropertyT<T> Create(PropertyKey a, T value) {
        //    if (a == null) throw new ArgumentNullException(nameof(a));
        //    if (value == null) throw new ArgumentNullException(nameof(value));
        //    var t = typeof(T);
        //    if (typeof(long).Equals(t)) return new Property(dummy: null) { _key = a, LngValue = (long)(object)value };
        //    if (typeof(int).Equals(t)) throw new TypeIntNotSupportedByAgoRapideException(nameof(value) + ": " + value);
        //    if (typeof(double).Equals(t)) return new Property(dummy: null) { _key = a, DblValue = (double)(object)value };
        //    if (typeof(bool).Equals(t)) return new Property(dummy: null) { _key = a, BlnValue = (bool)(object)value };
        //    if (typeof(DateTime).Equals(t)) return new Property(dummy: null) { _key = a, DtmValue = (DateTime)(object)value };
        //    if (typeof(string).Equals(t)) return new Property(dummy: null) { _key = a, StrValue = (string)(object)value };

        //    if (a.Key.A.Type == null) throw new NullReferenceException(
        //          "There is no " + nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " " +
        //          "defined for enum " + a.Key.A.Property.GetType() + "." + a.Key.A.Property + ". " +
        //          "Unable to assert whether " + typeof(T) + " (or rather " + value.GetType() + ") is valid for this enum");

        //    InvalidTypeException.AssertAssignable(typeof(T), a.Key.A.Type, () => nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " for " + a.Key.PExplained + " !IsAssignableFrom " + typeof(T));

        //    return new Property(dummy: null) { _key = a, _ADotTypeValue = value };
        //}

        /// <summary>
        /// Do not use directly. Use through <see cref="ParseResult.Create{T}"/> and <see cref="BaseEntity.AddProperty{T}"/>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public PropertyT(PropertyKey key, T value) : this(key, value, null) { }

        /// <summary>
        /// Preferred overload if <paramref name="strValue"/> is known by caller. 
        /// 
        /// Note that <see cref="T"/> equal to <see cref="object"/> is tolerated, and in most cases quite OK since the generic
        /// subclass <see cref="PropertyT{T}"/> is little used in AgoRapide anyway.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="strValue">
        /// </param>
        public PropertyT(PropertyKey key, T value, string strValue) : base(dummy: null) {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));
            _value = value;
            _genericValue = value;

            if (typeof(string).Equals(typeof(T))) {
                /// Do not bother with type checking now against <see cref="AgoRapideAttribute.Type"/>
                /// But note that <see cref="Property.TryGetV{T}"/> will fail at a later stage if called with something other than string. 
                /// Note that this is a pragmatic decision. 
                /// TODO: Consider tightening up
                /// TODO: Typical example for when needed now is code like this:
                /// TODO:    request.Result.AddProperty(CoreP.SuggestedUrl.A(), string.Join("\r\n", cmds.Select(cmd => request.CreateAPIUrl(cmd))));
                /// TODO: List[Uri] would be correct here
            } else {
                /// We can not do this:
                ///   InvalidTypeException.AssertAssignable(typeof(T), key.Key.A.Type, () => nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " for " + key.Key.PExplained + " !IsAssignableFrom " + typeof(T));
                /// because often we are called with T = object like from here (See <see cref="AgoRapideAttributeEnriched"/>):
                ///   if (Util.EnumTryParse(A.Type, value, out var temp)) return ParseResult.Create(this, temp);
                /// Instead we must do like this, using the actual type that we got:
                InvalidTypeException.AssertAssignable(value.GetType(), key.Key.A.Type, () => nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " for " + key.Key.PExplained + " !IsAssignableFrom " + value.GetType());
            }
            _stringValue = strValue ?? new Func<string>(() => {
                /// This is typically the case when called from <see cref="ParseResult.Create"/>        
                /// Note how <see cref="ParseResult.Create{T}"/> does not give us <paramref name="strValue"/> even though it would have
                /// been easy for it again to have that as an incoming parameter. But that string value would not necessarily be
                /// the same as <paramref name="value"/>.ToString() (the actual parser being used may accept very different input)

                // switch (value) // TODO: Why can we switchon value directly? http://stackoverflow.com/questions/41436399/expression-of-type-t-cannot-be-handled-by-a-pattern-of-type-x
                var objValue = (object)value;
                switch (objValue) {
                    case double dblValue: return dblValue.ToString2();
                    case DateTime dtmValue: return dtmValue.ToString(DateTimeFormat.DateHourMin);
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