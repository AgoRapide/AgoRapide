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
    /// TOOD: Change <see cref="Property"/> into a generic class and stop using internal properties like <see cref="LngValue"/>, <see cref="DblValue"/> and so on?
    /// TODO: At least, create a generic sub class which only sets <see cref="_ADotTypeValue"/> value for this class.
    /// TODO: leaving a much simpler understanding of the non-generic super class which is the one that will mainly be used.
    /// TODO: (This would correspond to how <see cref="AgoRapideAttributeEnrichedT{T}"/> and <see cref="AgoRapideAttributeEnriched"/> are being used now (Apr 2017)
    /// 
    /// You should normally not directly call initializations methods here (like <see cref="Create{T}(TProperty, T)"/>)  
    /// but instead rely on methods like <see cref="IDatabase.TryGetPropertyById"/> and <see cref="BaseEntityT.AddProperty"/>
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
        /// is not as nice as it is more natural to link to a property with only <see cref="KeyDB"/> in link text instead of "Key = Value"
        /// </summary>
        public override string Name => KeyDB;

        private string _keyDB;
        /// <summary>
        /// Key as stored in database
        /// </summary>
        public string KeyDB {
            get => _keyDB ?? (_keyDB = _key?.ToString() ?? throw new NullReferenceException(nameof(Key) + ". Either " + nameof(Key) + " or " + nameof(_keyDB) + " must be set from 'outside'"));
            set => _keyDB = value;
        }

        private PropertyKey _key;
        public PropertyKey Key => _key ?? (_key = PropertyKey.Parse(_keyDB ?? throw new NullReferenceException(nameof(_keyDB) + ". Either " + nameof(_key) + " or " + nameof(_keyDB) + " must be set from 'outside'"), () => ToString()));
        /// <summary>
        /// Key for use in HTML-code (as identifiers for use by Javascript)
        /// (that is, NOT key in HTML-format)
        /// 
        /// TODO: Make id more specific because saving for instance will now fail 
        /// TODO: for multiple properties on same HTML page with same <see cref="KeyDB"/>
        /// </summary>
        public string KeyHTML => KeyDB.ToString();

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

        /// <summary>
        /// Signifies that property has not been initialized properly. 
        /// Methods like <see cref="V{T}"/> and properties like <see cref="ValueA"/> should for instance not be called. 
        /// TODO: Replace with a separate property class called PropertyTemplate which will be the super class of this class
        /// </summary>
        public bool IsTemplateOnly { get; set; }
        public void AssertIsTemplateOnly() {
            if (!true.Equals(IsTemplateOnly)) throw new AgoRapideAttribute.IsManyException("!" + nameof(IsTemplateOnly) + ": " + ToString());
        }

        /// <summary>
        /// Only relevant when corresponding <see cref="AgoRapideAttribute.IsMany"/> for <see cref="Key"/>
        /// Will usually correspond to <see cref="PropertyKey.Index"/> being 0.
        /// </summary>
        public bool IsIsManyParent { get; private set; }
        public void AssertIsManyParent() {
            if (!true.Equals(IsIsManyParent)) throw new AgoRapideAttribute.IsManyException("!" + nameof(IsIsManyParent) + ": " + ToString());
        }

        /// <summary>
        /// Example: If PhoneNumber#1 and PhoneNumber#2 exists then PhoneNumber#3 will be returned. 
        /// </summary>
        /// <returns></returns>
        public PropertyKey GetNextIsManyId() {
            AssertIsManyParent();
            var id = 1; while (Properties.ContainsKey((CoreP)(object)(int.MaxValue - id))) {
                id++; if (id > 1000) throw new AgoRapideAttribute.IsManyException("id " + id + ", limit is (somewhat artificially) set to 1000. " + ToString());
            }
            return new PropertyKey(Key.Key, id);
        }

        /// <summary>
        /// Do not use this constructor. It always throws an exception.
        /// </summary>
        public Property() => throw new InvalidPropertyException(
            "Do not use this constructor. " +
            "This parameterless public constructor only exists in order to satisfy restrictions in " +
            nameof(IDatabase) + " like \"where T : BaseEntityT, new()\" for " +
            nameof(IDatabase.TryGetEntities) + " and " +
            nameof(IDatabase.TryGetEntityById) + ". " +
            "(None of these actually use this constructor anyway because they redirect to " + nameof(IDatabase.TryGetPropertyById) + " when relevant)");

        /// <summary>
        /// Dummy constructor for internal use in order to being able to disable parameterless constructor above. 
        /// </summary>
        /// <param name="dummy"></param>
        private Property(object dummy) {
        }

        /// <summary>
        /// See <see cref="IsTemplateOnly"/> for documentation. 
        /// TODO: Make a class called PropertyTemplate instead of using <see cref="IsTemplateOnly" />
        /// 
        /// Note that does NOT call <see cref="Initialize"/> 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Property CreateTemplate(PropertyKey key, BaseEntityT parent) => new Property(dummy: null) { IsTemplateOnly = true, _key = key, Parent = parent, ParentId = parent.Id };

        /// <summary>
        /// Creates a new Property which will function as a parent for <see cref="AgoRapideAttribute.IsMany"/> properties.
        /// 
        /// Note that does NOT call <see cref="Initialize"/> 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Property CreateIsManyParent(PropertyKey a) => new Property(dummy: null) {
            _key = a,
            Properties = new Dictionary<CoreP, Property>(),
            IsIsManyParent = true
        };

        /// <summary>
        /// Used when reading from database.
        /// 
        /// Note that does NOT call <see cref="Initialize"/> 
        /// </summary>
        /// <returns></returns>
        public static Property Create(
            long id,
            DateTime created,
            long creatorId,
            long parentId,
            long foreignId,
            string keyDB,
            long? lngValue,
            double? dblValue,
            bool? blnValue,
            DateTime? dtmValue,
            string geoValue,
            string strValue,
            DateTime? valid,
            long? validatorId,
            DateTime? invalid,
            long? invalidatorId
            ) =>
                new Property(dummy: null) {
                    Id = id,
                    Created = created,
                    CreatorId = creatorId,
                    ParentId = parentId,
                    ForeignId = foreignId,
                    KeyDB = keyDB,
                    LngValue = lngValue,
                    DblValue = dblValue,
                    BlnValue = blnValue,
                    DtmValue = dtmValue,
                    GeoValue = geoValue,
                    StrValue = strValue,
                    Valid = valid,
                    ValidatorId = validatorId,
                    Invalid = invalid,
                    InvalidatorId = invalidatorId
                };

        /// <summary>
        /// Now how <see cref="BaseEntityT.AddProperty)"/> is preferred when adding property to entity
        /// 
        /// Note that does NOT call <see cref="Initialize"/> 
        /// 
        /// (since further setting av properties like <see cref="ParentId"/> is most probably needed first)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Property Create<T>(PropertyKey a, T value) {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (value == null) throw new ArgumentNullException(nameof(value));
            var t = typeof(T);
            if (typeof(long).Equals(t)) return new Property(dummy: null) { _key = a, LngValue = (long)(object)value };
            if (typeof(int).Equals(t)) throw new TypeIntNotSupportedByAgoRapideException(nameof(value) + ": " + value);
            if (typeof(double).Equals(t)) return new Property(dummy: null) { _key = a, DblValue = (double)(object)value };
            if (typeof(bool).Equals(t)) return new Property(dummy: null) { _key = a, BlnValue = (bool)(object)value };
            if (typeof(DateTime).Equals(t)) return new Property(dummy: null) { _key = a, DtmValue = (DateTime)(object)value };
            if (typeof(string).Equals(t)) return new Property(dummy: null) { _key = a, StrValue = (string)(object)value };

            if (a.Key.A.Type == null) throw new NullReferenceException(
                  "There is no " + nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " " +
                  "defined for enum " + a.Key.A.Property.GetType() + "." + a.Key.A.Property + ". " +
                  "Unable to assert whether " + typeof(T) + " (or rather " + value.GetType() + ") is valid for this enum");

            // typeof(T) is really irrelevant now because it T is "thrown away" when creating property.
            //if (!attributes.A.Type.IsAssignableFrom(typeof(T))) throw new InvalidTypeException(
            //    typeof(T), attributes.A.Type, nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " " +
            //    "defined for enum " + typeof(TProperty).ToString() + "." + key.ToString() + ". " +
            //    "is not IsAssignableFrom " + typeof(T) + " " +
            //    "(actual type for parameter " + nameof(value) + " is " + value.GetType() + ")");

            // Instead we can check for value.GetType instead
            InvalidTypeException.AssertAssignable(value.GetType(), a.Key.A.Type, () =>
                nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " " +
                "defined for enum " + a.Key.A.Property.GetType() + "." + a.Key.A.Property + ". " +
                "!IsAssignableFrom " + value.GetType() + " " +
                "(actual " + nameof(T) + " is " + typeof(T) + ")");

            return new Property(dummy: null) { _key = a, _ADotTypeValue = value };
        }

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
            var type = Key.Key.A.Type;
            if (type == null) {
                if (_ADotTypeValue == null) {
                    // TODO: This is NOT preferred! We are assuming string as default type
                    aDotTypeValue = V<string>(); // This will also set _ADotTypeValue for next call
                } else {
                    aDotTypeValue = _ADotTypeValue; // TODO: This is NOT preferred! We are just accepting whatever we already have. Clean up this!
                }
            } else if (_ADotTypeValue != null && _ADotTypeValue.GetType().Equals(type)) {
                // This is quite OK
                aDotTypeValue = _ADotTypeValue;
            } else if (type.Equals(typeof(long))) {
                aDotTypeValue = V<long>();
            } else if (type.Equals(typeof(double))) {
                aDotTypeValue = V<double>();
            } else if (type.Equals(typeof(bool))) {
                aDotTypeValue = V<bool>();
            } else if (type.Equals(typeof(DateTime))) {
                aDotTypeValue = V<DateTime>();
            } else if (type.Equals(typeof(string))) {
                aDotTypeValue = V<string>();
            } else if (type.Equals(typeof(Type))) {
                aDotTypeValue = V<Type>();
            } else if (type.Equals(typeof(Uri))) {
                aDotTypeValue = V<Uri>();
            } else if (type.IsEnum) {
                /// TODO: We should be able to call <see cref="V{T}"/> here also...
                /// Do not use Util.EnumParse, we want a more detailed exception message
                /// aDotTypeValue = Util.EnumParse(KeyA.A.Type, V<string>());
                /// Or rather, do not do this either:
                // aDotTypeValue = Util.EnumTryParse(KeyA.A.Type, V<string>(), out var temp) ? temp : throw new InvalidPropertyException("Unable to parse '" + V<string>() + "' as " + KeyA.A.Type + ". Details: " + ToString());
                /// But this:
                aDotTypeValue = Util.EnumTryParse(type, V<string>(), out var temp) ? temp : null; // Just give up if fails
            } else if (typeof(ITypeDescriber).IsAssignableFrom(type)) {
                aDotTypeValue = Key.Key.TryValidateAndParse(V<string>(), out var temp) ? temp.ObjResult : null;
            } else {
                throw new InvalidTypeException(type, "Not implemented. Details: " + ToString());
            }
            return aDotTypeValue != null;
        }

        public T V<T>() => TryGetV(out T retval) ? retval : throw new InvalidPropertyException("Unable to convert value '" + Value + "' to " + typeof(T).ToString() + ", A.Type: " + (Key.Key.A.Type?.ToString() ?? "[NULL]") + ". Was the Property-object correct initialized? Details: " + ToString());
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
                if (LngValue != null) { value = (T)(_ADotTypeValue = LngValue.ToString()); return true; }; /// TODO: Introduce a <see cref="AgoRapideAttribute"/>.LngFormat property here
                if (DblValue != null) { value = (T)(_ADotTypeValue = ((double)DblValue).ToString2()); return true; }; /// TODO: Introduce a <see cref="AgoRapideAttribute"/>.DblFormat property here
                if (DtmValue != null) { value = (T)(_ADotTypeValue = ((DateTime)DtmValue).ToString(Key.Key.A.DateTimeFormat)); return true; };
                if (BlnValue != null) { value = (T)(_ADotTypeValue = BlnValue.ToString()); return true; }; // TODO: Better ToString here!
                if (GeoValue != null) { value = (T)(_ADotTypeValue = GeoValue); return true; };
                if (StrValue != null) { value = (T)(_ADotTypeValue = StrValue); return true; };

                if (_ADotTypeValue != null) { // TODO: Is this code relevant? Should not the specific value have been set anyway?
                    // The ToString-representation is not very helpful in some cases (Type, DateTime and so on
                    // Therefore we check for those first
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
                if (Key.Key.TryValidateAndParse(StrValue, out var result)) {
                    /// Note that TryValidateAndParse returns TRUE if no ValidatorAndParser is available
                    /// TODO: This is not good enough. FALSE would be a better result (unless TProperty is string)
                    /// TODO: because result.Result will now be set to a String value
                    /// TODO: Implement some kind of test here and clean up the whole mechanism
                    if (result.Result.StrValue != null) {
                        throw new InvalidPropertyException(
                            "Unable to cast '" + StrValue + "' to " + t + ", " +
                            "ended up with " + result.Result.StrValue.GetType() + ".\r\n" +
                            (Key.Key.ValidatorAndParser != null ?
                                "Very unexpected since " + nameof(AgoRapideAttributeEnriched.ValidatorAndParser) + " was set" :
                                "Most probably because " + nameof(AgoRapideAttributeEnriched.ValidatorAndParser) + " was not set"
                            ) + ".\r\n" +
                            "Details: " + ToString());
                    }
                    if (!(result.Result.ADotTypeValue() is T)) throw new InvalidPropertyException(
                        "Unable to cast '" + StrValue + "' to " + t + ", " +
                        "ended up with " + result.Result.ADotTypeValue().GetType() + " (value: '" + result.Result.ADotTypeValue().ToString() + ").\r\n" +
                        (Key.Key.ValidatorAndParser == null ?
                            "Very unexpected since " + nameof(AgoRapideAttributeEnriched.ValidatorAndParser) + " was not set" :
                            "Most probably because " + nameof(AgoRapideAttributeEnriched.ValidatorAndParser) + " returns the wrong type of object"
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
            throw new NotImplementedException("T: " + typeof(T).ToString() + ", A.Type: " + (Key.Key.A.Type?.ToString() ?? "[NULL]") + ". Details:" + ToString());

            // TODO: Decide how to implement different types. Exception or not?
        }

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
            (typeof(Type).IsAssignableFrom(aDotTypeValue.GetType()) ? ((Type)aDotTypeValue).GetAgoRapideAttributeForClass() :
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
                if (DtmValue != null) { Value = ((DateTime)DtmValue).ToString(Key.Key.A.DateTimeFormat); return; }
                if (StrValue != null) { Value = StrValue; return; }
                if (_ADotTypeValue != null) { Value = _ADotTypeValue.ToString(); return; } // TODO: Better ToString here!
                if (true.Equals(Key.Key.A.IsStrict)) {
                    // TODO: Try to MAKE A.TryValidateAndParse GENERIC in order for it to return a more strongly typed result.
                    if (!Key.Key.TryValidateAndParse(Value, out var parseResult)) throw new InvalidPropertyException(parseResult.ErrorResponse + ". Details: " + ToString());
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
            if (Key.Key.ValidatorAndParser != null) {
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

        /// <summary>
        /// Note: This method must be absolutely failsafe since it provides debug information.
        /// Note: Make sure it never introduces any recursivity or chance for exceptions being thrown.
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            nameof(ParentId) + ": " + ParentId + ", " +
            nameof(KeyDB) + ": " + (_keyDB ?? "[NULL]") + ", " +
            nameof(Key.Key.CoreP) + ": " + (_key?.Key.PExplained ?? "[NULL]") + ", " +
            nameof(Value) + ": " + (Value ?? "[NULL]") + ", " +
            nameof(LngValue) + ": " + (LngValue?.ToString() ?? "[NULL]") + ", " +
            nameof(DblValue) + ": " + (DblValue?.ToString() ?? "[NULL]") + ", " +
            nameof(BlnValue) + ": " + (BlnValue?.ToString() ?? "[NULL]") + ", " +
            nameof(DtmValue) + ": " + (DtmValue?.ToString() ?? "[NULL]") + ", " +
            nameof(GeoValue) + ": " + (GeoValue?.ToString() ?? "[NULL]") + ", " +
            nameof(StrValue) + ": " + (StrValue ?? "[NULL]") +
            (_key == null ? "" : (", " + nameof(Key.Key.A.Type) + ": " + (_key.Key.A.Type?.ToString() ?? "[NULL]"))) + ". "
            + base.ToString();

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
            var a = Key.Key.A;
            return "<tr><td>" +

                // --------------------
                // Column 1, Key
                // --------------------
                (string.IsNullOrEmpty(a.Description) ? "" : "<span title=\"" + a.Description.HTMLEncode() + "\">") +
                (Id <= 0 ? Name.HTMLEncode() : request.CreateAPILink(this)) +
                (string.IsNullOrEmpty(a.Description) ? "" : " (+)</span>") +
                "</td><td>" +

                // --------------------
                // Column 2, Value
                // --------------------
                (IsTemplateOnly || string.IsNullOrEmpty(ValueA.Description) ? "" : "<span title=\"" + ValueA.Description.HTMLEncode() + "\">") +
                ((!IsChangeableByCurrentUser || a.ValidValues != null) ?
                    // Note how passwords are not shown (although they are stored salted and hashed and therefore kind of "protected" we still do not want to show them)
                    (a.IsPassword ? "[SET]" : (IsTemplateOnly ? "" : V<string>().HTMLEncodeAndEnrich(request))) :
                    (
                        "<input " + // TODO: Vary size according to attribute.
                            "id=\"input_" + KeyHTML + "\"" +
                            (!a.IsPassword ? "" : " type=\"password\"") +
                            " value=\"" + (IsTemplateOnly || a.IsPassword ? "" : V<string>().HTMLEncode()) + "\"" +
                        "/>" +
                        "<label " +
                            "id=\"error_" + KeyHTML + "\"" +
                        ">" +
                        "</label>"
                    )
                ) +
                (IsTemplateOnly || string.IsNullOrEmpty(ValueA.Description) ? "" : " (+)</span>") +
                "</td><td>" +

                // --------------------
                // Column 3, Save button or SELECT
                // --------------------
                (!IsChangeableByCurrentUser ? "&nbsp;" :
                    (a.ValidValues == null ?
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
                            string.Join("\r\n", a.ValidValues.Select(v => "<option value=\"" + v + "\">" + v.HTMLEncode() + "</option>")) +
                            "</select>"
                        )
                    )
                ) +
                "</td><td>" +

                // --------------------
                // Column 4, Created
                // --------------------
                (Created.Equals(default(DateTime)) ? "&nbsp;" : Created.ToString(DateTimeFormat.DateHourMin)) +
                "</td><td>" +

                // --------------------
                // Column 5, Invalid
                // --------------------
                (Invalid == null ? "&nbsp;" : ((DateTime)Invalid).ToString(DateTimeFormat.DateHourMin)) +
                "</td></tr>\r\n\r\n";
        }

        public override string ToHTMLDetailed(Request request) {
            var retval = new StringBuilder();
            retval.AppendLine("<table><tr><th>Field</th><th>Value</th></tr>");
            var adder = new Action<DBField, string>((field, value) => {
                var A = field.GetAgoRapideAttributeT();
                var includeDescription = new Func<string>(() => {
                    switch (field) {
                        case DBField.key: if (!string.IsNullOrEmpty(A.A.Description)) return A.A.Description.HTMLEncode(); return null;
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
                var A = field.GetAgoRapideAttributeT();
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
            retval.AppendLine("<tr><td>Index</td><td>" + (Key.Index > 0 ? Key.Index.ToString() : "&nbsp;") + "</td></tr>\r\n");

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
            request.Result.AddProperty(CoreP.SuggestedUrl.A(), string.Join("\r\n", cmds.Select(cmd => request.CreateAPIUrl(cmd))));
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
                            p.Properties.Add(i.Value.Key.Key.PToString, i.Value.ToJSONProperty());
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
                    Key = Key.Key.PToString,
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

        /// <summary>
        /// The general value.
        /// Populated from one of lng, dbl, dtm, geo, strValue by Initialize
        /// 
        /// TODO: THIS IS CANDIDATE FOR REMOVAL. PROBABLY NOT NEEDED.
        /// 
        /// TODO: Rename into DebugValue. Set it from the future planned generic subclass of <see cref="Property"/>
        /// TODO: (Mar 2017 release of Visual Studio crashes when attempting to rename this property)
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// <see cref="DBField.lngv"/>
        /// </summary>
        private long? LngValue;
        /// <summary>
        /// <see cref="DBField.dblv"/>
        /// </summary>
        private double? DblValue;
        /// <summary>
        /// <see cref="DBField.blnv"/>
        /// </summary>
        private bool? BlnValue;
        /// <summary>
        /// <see cref="DBField.dtmv"/>
        /// </summary>
        private DateTime? DtmValue;
        /// <summary>
        /// <see cref="DBField.geov"/>
        /// </summary>
        private string GeoValue;
        /// <summary>
        /// <see cref="DBField.strv"/>
        /// </summary>
        private string StrValue;
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
        /// Is currently <see cref="AgoRapideAttributeEnriched.PToString"/>. Maybe change to ToStringShort or similar.
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

