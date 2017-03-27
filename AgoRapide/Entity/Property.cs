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
    /// You should normally not directly call initializations methods here (like <see cref="Create{T}(TProperty, T)"/>)  
    /// but instead rely on <see cref="IDatabase"/> or <see cref="BaseEntityT.AddProperty"/>
    /// in order to ensure correct population of fields like <see cref="ParentId"/> and <see cref="Parent"/> 
    /// and a proper call to <see cref="Property.Initialize"/>.
    /// </summary>
    [AgoRapide(
        Description = "Represents a single property of a -" + nameof(BaseEntityT) + "-.",
        LongDescription =
            "Note how -" + nameof(Property) + "- is itself also a -" + nameof(BaseEntityT) + "- and may therefore contain " +
            "a collection of -" + nameof(Property) + "- itself, either because it \"is\" -" + nameof(AgoRapideAttribute.IsMany) + "- or " +
            "because it just contains child-properties.",
        AccessLevelRead = AccessLevel.Relation,
        AccessLevelWrite = AccessLevel.Relation
    )]
    public class Property : BaseEntityT {

        /// <summary>
        /// Note that the alternative 
        ///   public override string Name => KeyDB + " = " + Value;
        /// is not as nice as it is more natural to link to a property with <see cref="KeyDB"/> in link text. 
        /// </summary>
        public override string Name => KeyDB;

        /// <summary>
        /// TODO: DELETE? Replace with attribute.IsMany?
        /// </summary>
        public bool IsMany { get; set; }

        /// <summary>
        /// <see cref="DBField.lngv"/>
        /// </summary>
        public long? LngValue { private get; set; }
        /// <summary>
        /// <see cref="DBField.dblv"/>
        /// </summary>
        public double? DblValue { private get; set; }
        /// <summary>
        /// <see cref="DBField.blnv"/>
        /// </summary>
        public bool? BlnValue { private get; set; }
        /// <summary>
        /// <see cref="DBField.dtmv"/>
        /// </summary>
        public DateTime? DtmValue { private get; set; }
        /// <summary>
        /// <see cref="DBField.geov"/>
        /// </summary>
        public string GeoValue { private get; set; }
        /// <summary>
        /// <see cref="DBField.strv"/>
        /// </summary>
        public string StrValue { private get; set; }

        // Removed TypValue since it will not originate from the database (it does not correspond to a DBField-enum)
        // Replaced with ADotTypeValue
        // TODO: Maybe we need this?
        //private Type _typValue = null;
        ///// <summary>
        ///// Returns the type representing this property.
        ///// Made private in order to force reading through V[Type], TryGetV[Type]
        ///// 
        //// TODO: Create some check for TypValue here. Now it just throws exception if invalid
        ///// </summary>
        //private Type TypValue => _typValue ?? (_typValue = new Func<Type>(() => {
        //    // Not this one:
        //    // if (A.A.Type != null) return A.A.Type;
        //    // throw new NotImplementedException();
        //    // but this one:
        //    return Util.GetTypeFromString(StrValue);
        //})());


        /// <summary>
        /// Should only be used by an <see cref="IDatabase"/> implementation
        /// </summary>
        public Property() {
        }

        /// <summary>
        /// Should only be used by an <see cref="IDatabase"/> implementation
        /// TODO: Why?
        /// 
        /// TODO: Is this only used by <see cref="CreateIsManyParent"/>? In that case we may make it private
        /// </summary>
        public Property(CoreProperty keyT) => _keyT = keyT;
        
        public bool IsIsManyParent { get; private set; }
        public static Property CreateIsManyParent(CoreProperty key) => new Property(key) {
            Properties = new Dictionary<CoreProperty, Property>(),
            IsIsManyParent = true
        };

        public void AssertIsManyParent() {
            if (!true.Equals(IsIsManyParent)) throw new AgoRapideAttribute.IsManyException("!" + nameof(IsIsManyParent) + ": " + ToString());
        }

        public int GetNextIsManyId() {
            AssertIsManyParent();
            var id = 1; while (Properties.ContainsKey((CoreProperty)(object)(int.MaxValue - id))) {
                id++; if (id > 1000) throw new AgoRapideAttribute.IsManyException("id " + id + ", limit is (somewhat artificially) set to 1000. " + ToString());
            }
            return id;
        }

        /// <summary>
        /// Use with caution. <see cref="BaseEntityT.AddProperty)"/> is preferred when adding property to entity
        /// 
        /// Note that does NOT call <see cref="Initialize"/> (since further setting av properties like <see cref="ParentId"/> 
        /// is most probably needed first)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Property Create<T>(CoreProperty key, T value) {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var t = typeof(T);
            if (typeof(long).Equals(t)) return new Property(key, (long)(object)value);
            if (typeof(int).Equals(t)) throw new TypeIntNotSupportedByAgoRapideException(nameof(value) + ": " + value);
            if (typeof(double).Equals(t)) return new Property(key, (double)(object)value);
            if (typeof(bool).Equals(t)) return new Property(key, (bool)(object)value);
            if (typeof(DateTime).Equals(t)) return new Property(key, (DateTime)(object)value);
            if (typeof(string).Equals(t)) return new Property(key, (string)(object)value);

            var attributes = key.GetAgoRapideAttribute();
            if (attributes.A.Type == null) throw new NullReferenceException(
                  "There is no " + nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " " +
                  "defined for enum " + typeof(CoreProperty).ToString() + "." + key.ToString() + ". " +
                  "Unable to assert whether " + typeof(T) + " (or rather " + value.GetType().ToString() + ") is valid for this enum");

            // typeof(T) is really irrelevant now because it T is "thrown away" when creating property.
            //if (!attributes.A.Type.IsAssignableFrom(typeof(T))) throw new InvalidTypeException(
            //    typeof(T), attributes.A.Type, nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " " +
            //    "defined for enum " + typeof(TProperty).ToString() + "." + key.ToString() + ". " +
            //    "is not IsAssignableFrom " + typeof(T) + " " +
            //    "(actual type for parameter " + nameof(value) + " is " + value.GetType() + ")");

            // Instead we can check for value.GetType instead
            InvalidTypeException.AssertAssignable(value.GetType(), attributes.A.Type, () =>
                nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " " +
                "defined for enum " + typeof(CoreProperty) + "." + key + ". " +
                "!IsAssignableFrom " + value.GetType() + " " +
                "(actual " + nameof(T) + " is " + typeof(T) + ")");

            return new Property(key, (object)value); // (object) clarifies which constructor we call now
                                                     // })().Initialize();
        }

        /// <summary>
        /// Use with caution. <see cref="BaseEntityT.AddProperty)"/> is preferred when adding property to entity
        /// </summary>
        /// <param name="key"></param>
        /// <param name="objValue"></param>
        public Property(CoreProperty key, object objValue) {
            _keyT = key;
            _ADotTypeValue = objValue;
        }

        /// <summary>
        /// Use with caution. <see cref="BaseEntityT.AddProperty)"/> is preferred when adding property to entity
        /// TODO: Make private anyway
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public Property(CoreProperty key, long value) {
            _keyT = key;
            LngValue = value;
        }

        /// <summary>
        /// Use with caution. <see cref="BaseEntityT.AddProperty)"/> is preferred when adding property to entity
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public Property(CoreProperty key, double value) {
            _keyT = key;
            DblValue = value;
        }

        /// <summary>
        /// Use with caution. <see cref="BaseEntityT.AddProperty)"/> is preferred when adding property to entity
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public Property(CoreProperty key, bool value) {
            _keyT = key;
            BlnValue = value;
        }

        /// <summary>
        /// Use with caution. <see cref="BaseEntityT.AddProperty)"/> is preferred when adding property to entity
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public Property(CoreProperty key, DateTime value) {
            _keyT = key;
            DtmValue = value;
        }

        // TOOD: Implement constructor for geo-value
        ///// <summary>
        /// Use with caution. <see cref="BaseEntityT.AddProperty)"/> is preferred when adding property to entity
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="value"></param>
        //public Property(TProperty key, string value) {
        //    _keyT = key;
        //    GeoValue = value;
        //    Initialize();
        //}

        /// <summary>
        /// Use with caution. <see cref="BaseEntityT.AddProperty)"/> is preferred when adding property to entity
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public Property(CoreProperty key, string value) {
            _keyT = key;
            StrValue = value;
        }

        /// <summary>
        /// <see cref="DBField.cid"/>
        /// </summary>
        public long CreatorId;

        /// <summary>
        /// <see cref="DBField.pid"/>
        /// </summary>
        public long ParentId;

        /// <summary>
        /// Will normally be set for ordinary properties (through DB-class) since we usually read properties
        /// in order to populate a BaseEntity, and therefore can set Parent without any performance hit
        /// 
        /// For "ordinary" properties this points to the actual parent-entity which they belong to, not to their
        /// parent-property. Note that the id's will correspond anyway, that is, ParentId will be the same as Parent.PrimaryKey
        /// 
        /// For entity root properties this will usually be 0 (since pid is NULL in the database for those)
        /// 
        /// Note that could theoretically be two choices for parent, either the parent entity or the entity root property 
        /// but having the latter as parent is deemed quite unnatural
        /// </summary>
        public BaseEntityT Parent;

        /// <summary>
        /// <see cref="DBField.fid"/>
        /// </summary>
        public long ForeignId;

        /// <summary>
        /// Only relevant if this property is a relation
        /// May be null (even if property is a relation)
        /// </summary>
        public BaseEntity ForeignEntity;

        public bool IsValid => true;

        private string _keyDB;
        /// <summary>
        /// Key as stored in database
        /// 
        /// TODO: CLEAN UP HOW WE HANDLE IsMany-properties!
        /// For <see cref="AgoRapideAttribute.IsMany"/>-properties (#x-properties) Key is given as stored in database (like member#1)
        /// For other properties Key will correspond to KeyT
        /// </summary>
        public string KeyDB {
            get => _keyDB ?? (_keyDB = new Func<string>(() => {
                if (_keyT == null) throw new NullReferenceException(nameof(_keyT) + ". Either " + nameof(_keyT) + " or " + nameof(_keyDB) + " must be set from 'outside'");
                if (Enum.IsDefined(typeof(CoreProperty), _keyT)) {
                    return _keyT.ToString();
                } else {
                    // This is a silently mapped CoreProperty. Use the CoreProperty value instead since _keyT.ToString() will be an "integer".
                    return _cpm.MapReverse((CoreProperty)_keyT).ToString();
                }
            })());
            set => _keyDB = value;
        }

        private CoreProperty? _keyT;
        /// <summary>
        /// TODO: CLEAN UP HOW WE HANDLE IsMany-properties!
        /// 
        /// Will return integer 0 value (corresponding to .None) if valid value not found
        /// (properties may very well have a Key that is not defined as TProperty)
        /// TOOD: IS THAT STILL TRUE? Feb 2017.
        /// 
        /// Note for <see cref="AgoRapideAttribute.IsMany"/>-properties neither <see cref="KeyT"/> nor <see cref="KeyDB"/> 
        /// will correspond to what is stored in <see cref="DBField.key"/>. <see cref="DBField.key"/> will be something like 
        /// "member#3" which will be split into a <see cref="AgoRapideAttribute.IsMany"/>-parent as "member" and child as 3.
        /// 
        /// Do not use <see cref="KeyT"/>.ToString() in your code, use instead <see cref="KeyDB"/> because for silently mapped 
        /// <see cref="CoreProperty"/>-values this will give you the <see cref="CoreProperty"/>-value ToString() instead of an "integer".
        /// </summary>
        public CoreProperty KeyT =>
            _keyT != null ? (CoreProperty)_keyT : (CoreProperty)(_keyT = new Func<CoreProperty>(() => {
                if (_keyDB == null) throw new NullReferenceException(nameof(_keyDB) + ". Either " + nameof(_keyT) + " or " + nameof(_keyDB) + " must be set from 'outside'");
                /// TOOD: IS THAT STILL RELEVANT AFTER INTRODUCING IsMany-parents?

                var retval = CorePropertyMapper.Map2(KeyDB);
                _keyA = retval.a; // Set keyA 
                return retval.cp;

                // CREATE TryMap2 above! 
                //if (Util.EnumTryParse(KeyDB, out CoreProperty retval)) return retval;
                //if (Util.EnumTryParse(KeyDB, out CoreProperty corePropertyTemp)) return _cpm.Map(corePropertyTemp);
                //var t = KeyDB.Split('#');
                //if (t.Length != 2) return default(TProperty);
                //if (!Util.EnumTryParse(t[0], out retval)) {
                //    if (!Util.EnumTryParse(t[0], out corePropertyTemp)) return default(TProperty);
                //    retval = _cpm.Map(corePropertyTemp);
                //}
                //if (!int.TryParse(t[1], out var temp)) return default(TProperty);
                //_multipleIndex = temp;
                //return retval;
            })());

        private AgoRapideAttributeT _keyA;
        /// <summary>
        /// TODO: Note how call to KeyT will also set _keyA... Should be done much better.
        /// </summary>
        public AgoRapideAttributeT KeyA => _keyA ?? (_keyA = KeyT.GetAgoRapideAttribute()); 

        //private AgoRapideAttributeT _keyAttribute;
        ///// <summary>
        ///// TODO: REPLACE WITH Attribute as found by <see cref="KeyT"/>
        ///// </summary>
        //public AgoRapideAttributeT KeyA => _keyAttribute ?? (_keyAttribute = KeyT.GetAgoRapideAttribute());

        /// TODO: CLEAN UP HOW WE HANDLE IsMany-properties!
        private int? _multipleIndex;
        /// <summary>
        /// TODO: CLEAN UP HOW WE HANDLE IsMany-properties!
        /// The 1-based index this property has as a multiple current property (#x-property)
        /// When stored in a dictionary the index <see cref="int.MaxValue"/> minus MultipleIndex is used.
        /// </summary>
        public int? MultipleIndex {
            get {
                if (_keyT == null) {
                    var dummy = KeyT; // This will also initialize _multipleIndex
                }
                return _multipleIndex;
            }
        }

        /// <summary>
        /// Key for use in HTML-code 
        /// (that is, NOT key in HTML-format)
        /// 
        /// TODO: Make id more specific because saving for instance will now fail 
        /// TODO: for multiple properties on same HTML page with same <see cref="KeyDB"/>
        /// </summary>
        public string KeyHTML => KeyDB.ToString();

        /// <summary>
        /// TODO: Might this replace all the others XXXValue-properties?
        /// 
        /// TODO: Change name into objValue maybe?
        /// </summary>
        private object _ADotTypeValue;
        /// <summary>
        /// Note how this throws an Exception if <see cref="_ADotTypeValue"/> is not set. 
        /// Property should be used with caution. Usually used from <see cref="Parameters"/> when ... ????????? 
        /// TODO: DOCUMENT BETTER! USE BETTER!
        /// 
        /// TODO: Make much better. Try to avoid if-else-if-else-if ... below.
        /// TODO: Think through all this much better!
        /// </summary>
        public object ADotTypeValue() => TryGetADotTypeValue(out var retval) ? retval : throw new NullReferenceException(nameof(_ADotTypeValue) + ": This property is usually set from this constructor: public Property(TProperty keyT, object objValue). Details: " + ToString());
        public bool TryGetADotTypeValue(out object aDotTypeValue) {
            if (KeyA.A.Type == null) {
                if (_ADotTypeValue == null) {
                    // TODO: This is NOT preferred! We are assuming string as default type
                    aDotTypeValue = V<string>(); // This will also set _ADotTypeValue for next call
                } else {
                    aDotTypeValue = _ADotTypeValue; // TODO: This is NOT preferred! We are just accepting whatever we already have. Clean up this!
                }
            } else if (_ADotTypeValue != null && _ADotTypeValue.GetType().Equals(KeyA.A.Type)) {
                // This is quite OK
                aDotTypeValue = _ADotTypeValue;
            } else if (KeyA.A.Type.Equals(typeof(long))) {
                aDotTypeValue = V<long>();
            } else if (KeyA.A.Type.Equals(typeof(double))) {
                aDotTypeValue = V<double>();
            } else if (KeyA.A.Type.Equals(typeof(bool))) {
                aDotTypeValue = V<bool>();
            } else if (KeyA.A.Type.Equals(typeof(DateTime))) {
                aDotTypeValue = V<DateTime>();
            } else if (KeyA.A.Type.Equals(typeof(string))) {
                aDotTypeValue = V<string>();
            } else if (KeyA.A.Type.Equals(typeof(Type))) {
                aDotTypeValue = V<Type>();
            } else if (KeyA.A.Type.Equals(typeof(Uri))) {
                aDotTypeValue = V<Uri>();
            } else if (KeyA.A.Type.IsEnum) {
                /// TODO: We should be able to call <see cref="V{T}"/> here also...
                /// Do not use Util.EnumParse, we want a more detailed exception message
                /// aDotTypeValue = Util.EnumParse(KeyA.A.Type, V<string>());
                /// Or rather, do not do this either:
                // aDotTypeValue = Util.EnumTryParse(KeyA.A.Type, V<string>(), out var temp) ? temp : throw new InvalidPropertyException("Unable to parse '" + V<string>() + "' as " + KeyA.A.Type + ". Details: " + ToString());
                /// But this:
                aDotTypeValue = Util.EnumTryParse(KeyA.A.Type, V<string>(), out var temp) ? temp : null; // Just give up if fails
            } else if (typeof(ITypeDescriber).IsAssignableFrom(KeyA.A.Type)) {
                aDotTypeValue = KeyA.TryValidateAndParse(V<string>(), out var temp) ? temp.ObjResult : null;
            } else {
                throw new InvalidTypeException(KeyA.A.Type, "Not implemented. Details: " + ToString());
            }
            return aDotTypeValue != null;
        }

        public T V<T>() => TryGetV(out T retval) ? retval : throw new InvalidPropertyException("Unable to convert value '" + Value + "' to " + typeof(T).ToString() + ", A.Type: " + (KeyA.A.Type?.ToString() ?? "[NULL]") + ". Was the Property-object correct initialized? Details: " + ToString());
        /// <summary>
        /// Result will be cached if T stays the same between each call
        /// 
        /// TODO: DOCUMENT / DECIDE BETTER ABOUT HANDLING OF CONVERSION PROBLEMS (INVALID VALUES AND SO ON)
        /// 
        /// TODO: Add an error / explanation or some other kind of response out response and communicate that all the
        /// TODO: way out to <see cref="BaseEntityT.TryGetPV{T}(TProperty, out T)"/> and so on.
        /// 
        /// TODO: Add corresponding lists of long/double/bool/Datetime/string and so on
        /// TODO: <see cref="AgoRapideAttribute.IsMany"/>-properties (#x-properties)
        /// TODO: This lists could be cached internally.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetV<T>(out T value) {
            var t = typeof(T);
            if (_ADotTypeValue != null && _ADotTypeValue is T) { value = (T)_ADotTypeValue; return true; } // Note how "as T" is not possible to use here

            if (typeof(object).Equals(t)) {
                if (_ADotTypeValue != null) { value = (T)_ADotTypeValue; return true; } // TODO: Check validity of all this!
                if (LngValue != null) { value = (T)(object)LngValue; return true; }
                if (DblValue != null) { value = (T)(object)DblValue; return true; }
                if (BlnValue != null) { value = (T)(object)BlnValue; return true; }
                if (DtmValue != null) { value = (T)(object)DtmValue; return true; }
                if (StrValue != null) { value = (T)(object)StrValue; return true; }
                throw new InvalidTypeException(
                    "Unable to find object.\r\n" +
                    "Details:\r\n" + ToString());
            }

            if (typeof(long).Equals(t)) {
                if (LngValue != null) { value = (T)(_ADotTypeValue = LngValue); return true; };
                value = default(T); return false;
            }
            if (typeof(int).Equals(t)) throw new TypeIntNotSupportedByAgoRapideException(ToString());

            if (typeof(double).Equals(t)) {
                if (DblValue != null) { value = (T)(_ADotTypeValue = DblValue); return true; };
                value = default(T); return false;
            }
            if (typeof(bool).Equals(t)) {
                if (BlnValue != null) { value = (T)(_ADotTypeValue = BlnValue); return true; };
                value = default(T); return false;
            }
            if (typeof(DateTime).Equals(t)) {
                if (DtmValue != null) { value = (T)(_ADotTypeValue = DtmValue); return true; };
                value = default(T); return false;
            }
            if (typeof(string).Equals(t)) {
                // TODO: CLEAN UP THIS. WHAT TO RETURN NOW? 
                //if (StrValue != null) return (T)(object)StrValue;

                //// TODO: DECIDE WHAT TO USE. String representation found in Initialize or in TryGetV
                // TODO: THIS IS NOT GOOD. The ToString-representation is not very helpful in a lot of cases (Type, DateTime and so on(
                //if (_ADotTypeValue != null) { value = (T)(object)_ADotTypeValue.ToString(); return true; };

                /// TODO: WHAT IS THE MEANING OF SETTING <see cref="_ADotTypeValue"/> now?
                /// TODO: WHAT IS THE PURPOSE OF SETTING IT TO A STRING? A STRING IS NOT WHAT IT IS SUPPOSED TO BE?
                if (LngValue != null) { value = (T)(_ADotTypeValue = LngValue.ToString()); return true; };
                if (DblValue != null) { value = (T)(_ADotTypeValue = ((double)DblValue).ToString2()); return true; };
                if (DtmValue != null) { value = (T)(_ADotTypeValue = ((DateTime)DtmValue).ToString(DateTimeFormat.DateHourMin)); return true; };
                if (BlnValue != null) { value = (T)(_ADotTypeValue = BlnValue.ToString()); return true; }; // TODO: Better ToString here!
                if (GeoValue != null) { value = (T)(_ADotTypeValue = GeoValue); return true; };
                if (StrValue != null) { value = (T)(_ADotTypeValue = StrValue); return true; };

                if (_ADotTypeValue != null) {
                    // The ToString-representation is not very helpful in some cases (Type, DateTime and so on
                    // There we check for thsoe first
                    if (_ADotTypeValue is double) { value = (T)(object)((double)_ADotTypeValue).ToString2(); return true; }
                    if (_ADotTypeValue is DateTime) { value = (T)(object)((DateTime)_ADotTypeValue).ToString(DateTimeFormat.DateHourMin); return true; }
                    // For all others we accept the default ToString conversion.
                    value = (T)(object)_ADotTypeValue.ToString(); return true;
                };

                // TODO: REPLACE WITH KIND OF "NO KNOWN TYPE OF PROPERTY VALUE FOUND"                
                throw new InvalidPropertyException("Unable to find string value. Details: " + ToString());
                // Do not return default(T) because we should always be able to convert a property to string
                // value = default(T); return false;                
            }
            if (typeof(Type).Equals(t)) {
                // TODO: Move typeof(Type).Equals(t) into AgoRapideAttribute.TryValidateAndParse instead. 
                if (Util.TryGetTypeFromString(StrValue, out var temp)) { value = (T)(_ADotTypeValue = temp); return true; }
                value = default(T); return false;
            }

            if (typeof(Uri).Equals(t)) {
                // TODO: Move typeof(Uri).Equals(t) into AgoRapideAttribute.TryValidateAndParse instead.                 
                if (Uri.TryCreate(StrValue, UriKind.RelativeOrAbsolute, out var temp)) { value = (T)(_ADotTypeValue = temp); return true; }
                value = default(T); return false;
            }

            if (StrValue != null) {
                // TODO: Try to MAKE A.TryValidateAndParse GENERIC in order for it to return a more strongly typed result.
                // TODO: Use that again to change its result-mechanism, not returning a property.
                if (KeyA.TryValidateAndParse(StrValue, out var result)) {
                    /// Note that TryValidateAndParse returns TRUE if no ValidatorAndParser is available
                    /// TODO: This is not good enough. FALSE would be a better result (unless TProperty is string)
                    /// TODO: because result.Result will now be set to a String value
                    /// TODO: Implement some kind of test here and clean up the whole mechanism
                    if (result.Result.StrValue != null) {
                        throw new InvalidPropertyException(
                            "Unable to cast '" + StrValue + "' to " + t + ", " +
                            "ended up with " + result.Result.StrValue.GetType() + ".\r\n" +
                            (KeyA.ValidatorAndParser != null ?
                                "Very unexpected since " + nameof(AgoRapideAttributeT.ValidatorAndParser) + " was set" :
                                "Most probably because " + nameof(AgoRapideAttributeT.ValidatorAndParser) + " was not set"
                            ) + ".\r\n" +
                            "Details: " + ToString());
                    }
                    if (!(result.Result.ADotTypeValue() is T)) throw new InvalidPropertyException(
                        "Unable to cast '" + StrValue + "' to " + t + ", " +
                        "ended up with " + result.Result.ADotTypeValue().GetType() + " (value: '" + result.Result.ADotTypeValue().ToString() + ").\r\n" +
                        (KeyA.ValidatorAndParser == null ?
                            "Very unexpected since " + nameof(AgoRapideAttributeT.ValidatorAndParser) + " was not set" :
                            "Most probably because " + nameof(AgoRapideAttributeT.ValidatorAndParser) + " returns the wrong type of object"
                        ) + ".\r\n" +
                        "Details: " + ToString());
                    value = (T)(_ADotTypeValue = result.Result.ADotTypeValue()); return true;
                }
                // TODO: Move typeof(Type).Equals(t) into AgoRapideAttribute.TryValidateAndParse instead. 
                value = default(T); return false;
            }

            // This code for enum-parsing does not belong here but in the A.TryValidateAndParse above
            //if (A.A.Type != null && t.Equals(A.A.Type)) {
            //    if (A.A.Type.IsEnum) {
            //        Util.EnumTryParse(A.A.Type, Value, out _ADotTypeValue);
            //        value = _ADotTypeValue == null ? default(T) : (T)_ADotTypeValue;
            //        return _ADotTypeValue != null;
            //    } else {
            //        throw new NotImplementedException("T: " + typeof(T).ToString() + ", A.A.Type: " + A.A.Type?.ToString() ?? "[NULL]");
            //    }
            //}
            throw new NotImplementedException("T: " + typeof(T).ToString() + ", A.Type: " + (KeyA.A.Type?.ToString() ?? "[NULL]") + ". Details:" + ToString());

            // TODO: Decide how to implement different types. Exception or not?
        }


        // TODO: CONSIDER MAKING THESE VALUES PRIVATE!
        // TODO: AND ALWAYS FORCING READING AS VAs<long> and so on (TryGetVAs<long>)

        /// <summary>
        /// The general value.
        /// Populated from one of lng, dbl, dtm, geo, strValue by Initialize
        /// 
        /// TODO: THIS IS CANDIDATE FOR REMOVAL. PROBABLY NOT NEEDED.
        /// </summary>
        public string Value { get; private set; }

        //private string _value;
        //private bool _valueHasAlreadyBeenSet = false;
        //public string Value {
        //    get => _value;
        //    set {
        //        if (_valueHasAlreadyBeenSet) { // Note that we do not take into consideration reading of cached values BEFORE Value has been set
        //                                       // Reset cached information
        //                                       // TODO: ADD HERE!
        //                                       //_valueAsDate = null;
        //                                       //_valueAsDouble = null;
        //                                       //_valueAsInteger = null;
        //        }
        //        _valueHasAlreadyBeenSet = true;
        //        _value = value;
        //    }
        //}

        /// <summary>
        /// <see cref="DBField.valid"/>
        /// </summary>
        public DateTime? Valid { get; set; }
        /// <summary>
        /// <see cref="DBField.vid"/>
        /// </summary>
        public long? ValidatorId { get; set; }

        /// <summary>
        /// <see cref="DBField.invalid"/>
        /// </summary>
        public DateTime? Invalid { get; set; }
        /// <summary>
        /// <see cref="DBField.iid"/>
        /// </summary>
        public long? InvalidatorId { get; set; }

        private AgoRapideAttribute _valueAttribute;
        /// <summary>
        /// Returns attributes for the value itself. 
        /// Usually used for giving helptext for the attribute. 
        /// 
        /// Requester is supposed to never change the 
        /// 
        /// Note that only a basic <see cref="AgoRapideAttribute"/>-object is returned, not an enriched <see cref="AgoRapideAttributeT"/>-object. 
        /// 
        /// NOTE: There are some difficulties involved in getting a <see cref="AgoRapideAttributeT"/>-object because of the generics involved. 
        /// We could use something like
        ///   if (aDotTypeValue.GetType().Equals(Typeof(APIMethodOrigin))) return V{ApiMethodOrigin}.GetAgoRapideAttribute() 
        ///   else if if (aDotTypeValue.GetType().Equals(... and so on
        ///   ...
        /// and also feeding a lambda for this through the configuration mechanism, so also client defined enums would be 
        /// supported, but it is very cumbersome and as of Feb 2017 no really need has materialised. 
        /// </summary>
        public AgoRapideAttribute ValueA => _valueAttribute ?? (_valueAttribute = TryGetADotTypeValue(out var aDotTypeValue) ?
            (aDotTypeValue.GetType().IsEnum ? aDotTypeValue.GetAgoRapideAttribute() :
            (typeof(Type).IsAssignableFrom(aDotTypeValue.GetType()) ? ((Type)aDotTypeValue).GetAgoRapideAttribute() :
            DefaultAgoRapideAttribute)) :
            DefaultAgoRapideAttribute);

        /// <summary>
        /// Use with caution. Note how the same instance is returned always. 
        /// Therefore the requester should not change this instance after "receiving" it. 
        /// </summary>
        public static AgoRapideAttribute DefaultAgoRapideAttribute = AgoRapideAttribute.GetNewDefaultInstance();

        /// <summary>
        /// Will validate according to attributes defined
        /// Returns itself for fluent purposes
        /// </summary>
        /// <returns></returns>
        public Property Initialize() {
            new Action(() => {
                // TODO: DECIDE WHAT TO USE. String representation found in Initialize or in TryGetV
                if (LngValue != null) { Value = LngValue.ToString(); return; }
                if (DblValue != null) { Value = ((double)DblValue).ToString2(); return; }
                if (BlnValue != null) { Value = ((bool)BlnValue).ToString(); return; }
                if (DtmValue != null) { Value = ((DateTime)DtmValue).ToString(KeyA.A.DateTimeFormat); return; }
                if (StrValue != null) { Value = StrValue; return; }
                if (_ADotTypeValue != null) { Value = _ADotTypeValue.ToString(); return; } // TODO: Better ToString here!
                if (true.Equals(KeyA.A.IsStrict)) {
                    // TODO: Try to MAKE A.TryValidateAndParse GENERIC in order for it to return a more strongly typed result.
                    if (!KeyA.TryValidateAndParse(Value, out var parseResult)) throw new InvalidPropertyException(parseResult.ErrorResponse + ". Details: " + ToString());
                    // TODO: This is difficult. Result._ADotTypeValue is most probably not set
                    _ADotTypeValue = parseResult.Result._ADotTypeValue;
                    // TODOk: FIX!!!
                }
                // We could parse at once, but it might never be needed so it is better to let TryGetV do it later
                // else if (A.Type == null && A.Type.IsEnum) {
                //    if (A.TryValidateAndParse(Value, out var parsedValue, out _)) _ADotTypeValue = parsedValue;
                //}
                // TODO: REPLACE WITH KIND OF "NO KNOWN TYPE OF PROPERTY VALUE FOUND"
                throw new InvalidPropertyException("Unable to find string value for " + ToString());
            })();
            if (KeyA.ValidatorAndParser != null) {
                // We could consider running the validator now if it was not already run, 
                // but it would be quite meaningless
                // if it is a standard TryParse for Long for instance, because LngValue is already set

                // TODO: Consider distinguishing between SyntactivalValidator and RangeValidator
                // TODO: In other words, validate for range 1-10 (after we have checked that is a long)
            }
            return this;
        }

        /// <summary>
        /// Note existence of both <see cref="Property.InvalidPropertyException"/> and <see cref="BaseEntityT.InvalidPropertyException{T}"/>
        /// </summary>
        private class InvalidPropertyException : ApplicationException {
            public InvalidPropertyException(string message) : base(message) { }
        }

        public override string ToString() =>
            nameof(ParentId) + ": " + ParentId + ", " +
            nameof(KeyDB) + ": " + KeyDB + ", " +
            nameof(Value) + ": " + Value + ", " +
            nameof(LngValue) + ": " + (LngValue?.ToString() ?? "[NULL]") + ", " +
            nameof(DblValue) + ": " + (DblValue?.ToString() ?? "[NULL]") + ", " +
            nameof(BlnValue) + ": " + (BlnValue?.ToString() ?? "[NULL]") + ", " +
            nameof(DtmValue) + ": " + (DtmValue?.ToString() ?? "[NULL]") + ", " +
            nameof(GeoValue) + ": " + (GeoValue?.ToString() ?? "[NULL]") + ", " +
            nameof(StrValue) + ": " + (StrValue ?? "[NULL]") + ", " +
            nameof(KeyA) + "." + nameof(KeyA.A) + "." +
            nameof(KeyA.A.Type) + ": " + (KeyA.A.Type?.ToString() ?? "[NULL]") + ". " + base.ToString();

        public override string ToHTMLTableHeading(Request request) => HTMLTableHeading;
        public const string HTMLTableHeading = "<tr><th>Key</th><th>Value</th><th>Save</th><th>" + nameof(Created) + "</th><th>" + nameof(Invalid) + "</th></tr>";

        /// <summary>
        /// Hack for transferring information from 
        /// <see cref="BaseEntityT.CreateHTMLForExistingProperties"/> and 
        /// <see cref="BaseEntityT.CreateHTMLForAddingProperties"/> to 
        /// <see cref="ToHTMLTableRow"/>. 
        /// Do not use except between these methods. 
        /// Hack implemented because of difficulty of adding parameter to <see cref="ToHTMLTableRow"/>. 
        /// </summary>
        public bool IsChangeableByCurrentUser;

        /// <summary>
        /// Signifies that property has not been initialized property. Methods like <see cref="V{T}"/> should for instance not be called. 
        /// TODO: Replace with a separate property class called PropertyTemplate which will be the super class of this class
        /// </summary>
        public bool IsTemplateOnly { get; set; }

        /// <summary>
        /// TODO: Create better links, use <see cref="CoreMethod"/> or similar in order to get the REAL URL's used by the actual methods.
        /// 
        /// Note that may return multiple rows if <see cref="IsIsManyParent"/>
        /// 
        /// TODO: BUILD UPON THIS. CENTRAL METHOD.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public override string ToHTMLTableRow(Request request) {
            if (IsIsManyParent) return string.Join("\r\n", Properties.Select(p => p.Value.ToHTMLTableRow(request)));
            return "<tr><td>" +

                // Key
                (string.IsNullOrEmpty(KeyA.A.Description) ? "" : "<span title=\"" + KeyA.A.Description.HTMLEncode() + "\">") +
                (Id <= 0 ? Name.HTMLEncode() : request.CreateAPILink(this)) +
                (string.IsNullOrEmpty(KeyA.A.Description) ? "" : " (+)</span>") +
                "</td><td>" +

                // Value
                (IsTemplateOnly || string.IsNullOrEmpty(ValueA.Description) ? "" : "<span title=\"" + ValueA.Description.HTMLEncode() + "\">") +
                ((!IsChangeableByCurrentUser || KeyA.A.ValidValues != null) ?
                    // Note how passwords are not shown (although they are stored salted and hashed and therefore kind of "protected" we still do not want to show them)
                    (KeyA.A.IsPassword ? "[SET]" : (IsTemplateOnly ? "" : V<string>().HTMLEncodeAndEnrich(request))) :
                    (
                        "<input " + // TODO: Vary size according to attribute.
                            "id=\"input_" + KeyHTML + "\"" +
                            (!KeyA.A.IsPassword ? "" : " type=\"password\"") +
                            " value=\"" + (IsTemplateOnly || KeyA.A.IsPassword ? "" : V<string>().HTMLEncode()) + "\"" +
                        "/>" +
                        "<label " +
                            "id=\"error_" + KeyHTML + "\"" +
                        ">" +
                        "</label>"
                    )
                ) +
                (IsTemplateOnly || string.IsNullOrEmpty(ValueA.Description) ? "" : " (+)</span>") +
                "</td><td>" +

                (!IsChangeableByCurrentUser ? "&nbsp;" :
                    (KeyA.A.ValidValues == null ?
                        (
                            // Ordinary textbox was presented. Add button.

                            /// Note: Corresponding Javascript method being called here is currently generated in <see cref="HTMLView.GetHTMLStart"/>

                            /// TODO: An alternative to the above would be to 
                            /// TODO: aonsider making <see cref="APIMethod"/> create Javascript such as this automatically...
                            /// TODO: In other words, call the <see cref="APIMethod"/> for <see cref="CoreMethod.UpdateProperty"/> 
                            /// TODO: in order to get the Javascript required here, instead of generating it as done immediately below:
                            "<input " +
                                "type=\"button\" " +
                                "value = \"Save\" " +
                                "onclick = \"try { " +
                                        CoreMethod.UpdateProperty + "('" + KeyHTML + "', '" + Parent.GetType().ToStringVeryShort() + "', '" + ParentId + "', '" + KeyDB + "'); " +
                                    "} catch (err) { " +
                                        "com.AgoRapide.AgoRapide.log(err); " +
                                    "} return false;" +
                                "\"" +
                            "/>"
                        ) : (

                            // Create select with valid values.
                            /// Note: Corresponding Javascript method being called here is currently generated in <see cref="HTMLView.GetHTMLStart"/>
                            "<select " +
                                "id=\"input_" + KeyHTML + "\" " +
                                "onchange = \"try { " +
                                        CoreMethod.UpdateProperty + "('" + KeyHTML + "', '" + Parent.GetType().ToStringVeryShort() + "', '" + ParentId + "', '" + KeyDB + "'); " +
                                    "} catch (err) { " +
                                        "com.AgoRapide.AgoRapide.log(err); " +
                                    "} return false;" +
                                "\"" +
                            "/>" +
                            /// TODO: Idea for <see cref="Property.ToHTMLTableRow(Request)"/>
                            /// TODO: SELECT values for choosing should also have PropertyOperation in them, se we can immediately
                            /// TODO: delete properties from the HTML admin interface.
                            /// TOOD: (but that would leave properties without <see cref="AgoRapideAttribute.ValidValues"/> without such...)
                            /// TODO: Maybe better to just leave as is...

                            "<option value=\"\">[Choose " + Name.HTMLEncode() + "...]</option>\r\n" +
                            /// TODO: Add to <see cref="AgoRapideAttribute.ValidValues"/> a List of tuples with description for each value
                            /// TODO: (needed for HTML SELECT tags)
                            string.Join("\r\n", KeyA.A.ValidValues.Select(v => "<option value=\"" + v + "\">" + v.HTMLEncode() + "</option>")) +
                            "</select>"
                        )
                    )
                ) +
                "</td><td>" +

                // Created
                (Created.Equals(default(DateTime)) ? "&nbsp;" : Created.ToString(DateTimeFormat.DateHourMin)) +
                "</td><td>" +

                // Invalid
                (Invalid == null ? "&nbsp;" : ((DateTime)Invalid).ToString(DateTimeFormat.DateHourMin)) +
                "</td></tr>\r\n\r\n";
        }

        public override string ToHTMLDetailed(Request request) {
            var retval = new StringBuilder();
            retval.AppendLine("<table><tr><th>Field</th><th>Value</th></tr>");
            var adder = new Action<DBField, string>((field, value) => {
                var A = field.GetAgoRapideAttribute();
                var includeDescription = new Func<string>(() => {
                    switch (field) {
                        case DBField.key: if (!string.IsNullOrEmpty(KeyA.A.Description)) return KeyA.A.Description.HTMLEncode(); return null;
                        case DBField.strv: if (!string.IsNullOrEmpty(ValueA.Description)) return ValueA.Description.HTMLEncode(); return null;
                        default: return null;
                    }
                })();

                retval.AppendLine("<tr><td>" +
                    (string.IsNullOrEmpty(A.A.Description) ? "" : "<span title=\"" + A.A.Description.HTMLEncode() + "\">") +
                    field.ToString() +
                    (string.IsNullOrEmpty(A.A.Description) ? "" : " (+)</span>") +
                    "</td><td>" +
                    (includeDescription == null ? "" : "<span title=\"" + includeDescription + "\">") +
                    (value?.HTMLEncodeAndEnrich(request) ?? "&nbsp;") +
                    (includeDescription == null ? "" : " (+)</span>") +
                    "</td></tr>");
            });
            var adderWithLink = new Action<DBField, long?>((field, value) => {
                var A = field.GetAgoRapideAttribute();
                retval.AppendLine("<tr><td>" +
                    (string.IsNullOrEmpty(A.A.Description) ? "" : "<span title=\"" + A.A.Description.HTMLEncode() + "\">") +
                    field.ToString() +
                    (string.IsNullOrEmpty(A.A.Description) ? "" : " (+)</span>") +
                    "</td><td>" +
                    (value != null && value != 0 ?
                        (Util.EntityCache.TryGetValue((long)value, out var entity) ?
                            request.CreateAPILink(entity) :
                            value.ToString()) :
                        "&nbsp;"
                    ) +
                    "</td></tr>");
            });

            adderWithLink(DBField.id, Id);
            adder(DBField.created, Created.ToString(DateTimeFormat.DateHourMinSec));
            adderWithLink(DBField.cid, CreatorId);
            adderWithLink(DBField.pid, ParentId);
            adderWithLink(DBField.fid, ForeignId);
            adder(DBField.key, KeyDB);

            // This one really was not necessary since we use KeyDB above
            // retval.AppendLine("<tr><td>" + nameof(DBField.key) + " (explained)</td><td>" + KeyA.PExplained + "</td></tr>\r\n");

            // TODO: Add helptext for this (or remove it).
            retval.AppendLine("<tr><td>Index</td><td>" + (MultipleIndex?.ToString() ?? "&nbsp;") + "</td></tr>\r\n");

            adder(DBField.lngv, LngValue?.ToString());
            adder(DBField.dblv, DblValue?.ToString());
            adder(DBField.blnv, BlnValue?.ToString());
            adder(DBField.dtmv, DtmValue?.ToString());
            adder(DBField.geov, GeoValue?.ToString());
            adder(DBField.strv, StrValue?.ToString());
            // retval.AppendLine("<tr><td>" + nameof(DBField.strv) + " (explained)</td><td>" + ValueA.Description + "</td></tr>\r\n");
            adder(DBField.valid, Valid?.ToString(DateTimeFormat.DateHourMinSec));
            adderWithLink(DBField.vid, ValidatorId);
            adder(DBField.invalid, Invalid?.ToString(DateTimeFormat.DateHourMinSec));
            adderWithLink(DBField.iid, InvalidatorId);
            retval.AppendLine("</table>");
            var cmds = new List<string>();
            request.CreateAPICommand(CoreMethod.History, GetType(), new IntegerQueryId(Id)).Use(cmd => {
                retval.AppendLine("<p>" + request.CreateAPILink(cmd, "History") + "</p>");
                cmds.Add(cmd);
            });
            Util.EnumGetValues<PropertyOperation>().ForEach(o => {
                request.CreateAPICommand(CoreMethod.PropertyOperation, GetType(), new IntegerQueryId(Id), o).Use(cmd => {
                    retval.AppendLine("<p>" + request.CreateAPILink(cmd, o.ToString()) + "</p>");
                    cmds.Add(cmd);
                });
            });
            request.Result.AddProperty(M(CoreProperty.SuggestedUrl), string.Join("\r\n", cmds.Select(cmd => request.CreateAPIUrl(cmd))));
            return base.ToHTMLDetailed(request).ReplaceWithAssert("<!--DELIMITER-->", retval.ToString());
        }
        /// <summary>
        /// For example of override see <see cref="BaseEntityTWithLogAndCount.ToJSONEntity"/> or <see cref="Property.ToJSONEntity"/>
        /// 
        /// Do not use this method, use more strongly typed <see cref="ToJSONProperty"/> instead.
        /// </summary>
        /// <returns></returns>
        public override JSONEntity0 ToJSONEntity(Request request) => ToJSONProperty();

        /// <summary>
        /// Should we have Request as parameter here, and do som <see cref="AccessLevel"/>-checking like for <see cref="ToJSONEntity"/>? 
        /// </summary>
        /// <returns></returns>
        public JSONProperty0 ToJSONProperty() {
            if (Id == 0 && CreatorId == 0 && ParentId == 0 && InvalidatorId == null && Invalid == null) {
                // TODO: Finish up here. Take into accounts other properties as well.
                return new JSONProperty0 { Value = V<string>() };
            } else {
                var propertyAdder = new Action<JSONProperty1>(p => {
                    if (Properties != null) {
                        p.Properties = new Dictionary<string, JSONProperty0>();
                        Properties.ForEach(i => {
                            p.Properties.Add(i.Key.GetAgoRapideAttribute().PToString, i.Value.ToJSONProperty());
                        });
                    }
                });

                //if (Invalid != null) {
                //    var retval = new JSONProperty4 {
                //        Id = Id, CreatorId = CreatorId, ParentId = ParentId, Key = KeyT.GetAgoRapideAttribute().PToString, Value = V<string>(),
                //        Created = Created,
                //        Valid = Valid,
                //        Invalid = (DateTime)Invalid,
                //    };
                //    propertyAdder(retval);
                //} else if (Valid!=null) { 

                //} else {
                // TODO: Finish up here. Take into accounts other properties as well. InvalidatorId, Invalid and so on.
                var retval = new JSONProperty1 {
                    Id = Id,
                    Created = Created,
                    CreatorId = CreatorId,
                    ParentId = ParentId,
                    Key = KeyT.GetAgoRapideAttribute().PToString,
                    Value = V<string>(),
                    Valid = Valid,
                    ValidatorId = ValidatorId,
                    Invalid = Invalid,
                    InvalidatorId = InvalidatorId

                };
                propertyAdder(retval);
                return retval;
                // }
            }
        }
    }

    /// <summary>
    /// JSONProperty0/1/2/3/4 contains gradually more and more information
    /// </summary>
    public class JSONProperty0 : JSONEntity0 {
        /// <summary>
        /// <see cref="Property.LngValue"/>
        /// <see cref="Property.DblValue"/>
        /// <see cref="Property.BlnValue"/>
        /// <see cref="Property.DtmValue"/>
        /// <see cref="Property.ADotTypeValue"/>
        /// <see cref="Property.StrValue"/>
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Used for debug purposes (therefore method, not property, in order for not to show up in ordinary JSON data)
        /// </summary>
        /// <returns></returns>
        public string GetValueShortened() => (Value?.Substring(0, Math.Min(255, Value.Length)) ?? "") + ((Value?.Length ?? int.MaxValue) < 255 ? "..." : "");
    }
    public class JSONProperty1 : JSONProperty0 {
        /// <summary>
        /// <see cref="BaseEntity.Id"/>
        /// </summary>
        public long Id { get; set; }
        public DateTime? Created { get; set; }
        /// <summary>
        /// <see cref="Property.CreatorId"/>
        /// </summary>
        public long CreatorId { get; set; }
        /// <summary>
        /// <see cref="Property.ParentId"/>
        /// </summary>
        public long ParentId { get; set; }
        /// <summary>
        /// <see cref="Property.ForeignId"/>
        /// </summary>
        public long ForeignId { get; set; }
        /// <summary>
        /// TODO: We should really consider if there is any point in this property, as it often shows up as
        /// key in containing JSON dictionary anyway.
        /// 
        /// Is currently <see cref="AgoRapideAttributeT.PToString"/>. Maybe change to ToStringShort or similar.
        /// </summary>
        public string Key { get; set; }
        public List<string> ValidValues;
        public Dictionary<string, JSONProperty0> Properties { get; set; }
        public DateTime? Valid { get; set; }
        public long? ValidatorId { get; set; }
        public DateTime? Invalid { get; set; }
        public long? InvalidatorId { get; set; }
        public JSONProperty1() {
        }
    }
    //public class JSONProperty2 : JSONProperty1 {
    //}
    //public class JSONProperty3 : JSONProperty2 {
    //}
    //public class JSONProperty4 : JSONProperty3 {
    //}
}

