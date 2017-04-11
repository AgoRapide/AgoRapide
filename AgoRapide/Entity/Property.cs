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
    /// but instead rely on methods like <see cref="IDatabase.TryGetPropertyById"/> and <see cref="BaseEntity.AddProperty"/>
    /// in order to ensure correct population of fields like <see cref="ParentId"/> and <see cref="Parent"/>.
    /// 
    /// This class is deliberately not made abstract in order to faciliate use of "where T: new()" constraint in method signatures like
    /// <see cref="IDatabase.TryGetEntities{T}"/> 
    /// 
    /// Subclass: <see cref="PropertyT{T}"/>
    /// </summary>
    [AgoRapide(
        Description = "Represents a single property of a -" + nameof(BaseEntity) + "-.",
        LongDescription =
            "Note how -" + nameof(Property) + "- is itself also a -" + nameof(BaseEntity) + "- and may therefore contain " +
            "a collection of -" + nameof(Property) + "- itself, either because it \"is\" -" + nameof(AgoRapideAttribute.IsMany) + "- or " +
            "because it just contains child-properties.",
        AccessLevelRead = AccessLevel.Relation,
        AccessLevelWrite = AccessLevel.Relation
    )]
    public class Property : BaseEntity {

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

        protected PropertyKey _key;
        public PropertyKey Key => _key ?? (_key = PropertyKey.Parse(_keyDB ?? throw new NullReferenceException(nameof(_keyDB) + ". Either " + nameof(_key) + " or " + nameof(_keyDB) + " must be set from 'outside'"), () => ToString()));
        /// <summary>
        /// Key for use in HTML-code (as identifiers for use by Javascript)
        /// (that is, NOT key in HTML-format)
        /// 
        /// TODO: Make id more specific because saving for instance will now fail 
        /// TODO: for multiple properties on same HTML page with same <see cref="KeyDB"/>
        /// </summary>
        public string KeyHTML => KeyDB.ToString().Replace("#", "_");

        protected string _stringValue;

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
        public BaseEntity Parent;

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
            if (!true.Equals(IsTemplateOnly)) throw new InvalidPropertyException("!" + nameof(IsTemplateOnly) + ": " + ToString());
        }

        /// <summary>
        /// Only relevant when corresponding <see cref="AgoRapideAttribute.IsMany"/> for <see cref="Key"/>
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
            nameof(IDatabase) + " like \"where T : BaseEntity, new()\" for " +
            nameof(IDatabase.TryGetEntities) + " and " +
            nameof(IDatabase.TryGetEntityById) + ". " +
            "(None of these actually use this constructor anyway because they redirect to " + nameof(IDatabase.TryGetPropertyById) + " when relevant)");

        /// <summary>
        /// Dummy constructor for internal use in order to being able to disable parameterless constructor above. 
        /// Used by <see cref="CreateTemplate"/> and <see cref="CreateIsManyParent"/> and by <see cref="PropertyT{T}"/>
        /// </summary>
        /// <param name="dummy"></param>
        protected Property(object dummy) {
        }

        /// <summary>
        /// See <see cref="IsTemplateOnly"/> for documentation. 
        /// TODO: Make a class called PropertyTemplate instead of using <see cref="IsTemplateOnly" />
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Property CreateTemplate(PropertyKey key, BaseEntity parent) => new Property(dummy: null) {
            IsTemplateOnly = true,
            _key = key,
            Parent = parent,
            ParentId = parent.Id
        };

        /// <summary>
        /// See <see cref="IsIsManyParent"/> for documentation. 
        /// TODO: Make a class called PropertyTemplate instead of using <see cref="IsIsManyParent" />
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Property CreateIsManyParent(PropertyKey a) => new Property(dummy: null) {
            IsIsManyParent = true,
            _key = a,
            Properties = new Dictionary<CoreP, Property>()
        };

        /// <summary>
        /// Used when reading from database.
        /// 
        /// Note that does NOT call <see cref="Initialize"/>. 
        /// 
        /// Note how internally creates a <see cref="PropertyT{T}"/> object. 
        /// </summary>
        /// <returns></returns>
        public static Property Create(
            PropertyKey key,
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
            ) {

            var retval = new Func<Property>(() => {
                // TODO: DECIDE WHAT TO USE. String representation found in Initialize or in TryGetV
                if (lngValue != null) return new PropertyT<long>(key, (long)lngValue, lngValue.ToString());
                if (dblValue != null) return new PropertyT<double>(key, (double)dblValue, ((double)dblValue).ToString2());
                if (blnValue != null) return new PropertyT<bool>(key, (bool)blnValue, ((bool)blnValue).ToString());
                if (dtmValue != null) return new PropertyT<DateTime>(key, (DateTime)dtmValue, ((DateTime)dtmValue).ToString(key.Key.A.DateTimeFormat));
                if (strValue == null) throw new NullReferenceException("None of the following was set: " + nameof(lngValue) + ", " + nameof(dblValue) + ", " + nameof(blnValue) + ", " + nameof(dtmValue) + ", " + nameof(strValue) + ".\r\nDetails: " + key.Key.ToString());
                if (key.Key.A.Type.Equals(typeof(string))) return new PropertyT<string>(key, strValue, strValue);
                if (!key.Key.TryValidateAndParse(strValue, out var parseResult)) {
                    if (key.Key.A.IsNotStrict) return new PropertyT<string>(key, strValue, strValue);
                    throw new InvalidPropertyException(
                        parseResult.ErrorResponse + ".\r\n" +
                        "Possible resolution:\r\n" +
                        "  DELETE FROM p WHERE " + DBField.id + " = " + id + "\r\n" +
                        "Details: " + key.Key.ToString());
                }
                return parseResult.Result;
            })();

            retval.Id = id;
            retval.Created = created;
            retval.CreatorId = creatorId;
            retval.ParentId = parentId;
            retval.ForeignId = foreignId;
            retval.KeyDB = keyDB;
            retval.Valid = valid;
            retval.ValidatorId = validatorId;
            retval.Invalid = invalid;
            retval.InvalidatorId = invalidatorId;

            return retval;
        }

        /// <summary>
        /// TODO: Fix name for this!
        /// </summary>
        public object _value { get; protected set; }
        /// <summary>
        /// The generic value for this property. Corresponds to <see cref="PropertyT{T}._genericValue"/>
        /// </summary>
        public object Value => _value ?? throw new NullReferenceException(nameof(_value) + "Details: " + ToString());

        public T V<T>() => TryGetV(out T retval) ? retval : throw new InvalidPropertyException("Unable to convert value '" + _stringValue + "' to " + typeof(T).ToString() + ", A.Type: " + (Key.Key.A.Type?.ToString() ?? "[NULL]") + ". Was the Property-object correct initialized? Details: " + ToString());
        public bool TryGetV<T>(out T value) {
            if (_value != null && _value is T) { value = (T)_value; return true; } // Note how "as T" is not possible to use here

            var t = typeof(T);

            if (typeof(string).Equals(t)) { value = (T)(object)(_stringValue ?? throw new NullReferenceException(nameof(_stringValue))); return true; }

            //if (typeof(object).Equals(t)) {
            //    if (_ADotTypeValue != null) { value = (T)_ADotTypeValue; return true; } // TODO: Check validity of all this!
            //    if (LngValue != null) { value = (T)(object)LngValue; return true; }
            //    if (DblValue != null) { value = (T)(object)DblValue; return true; }
            //    if (BlnValue != null) { value = (T)(object)BlnValue; return true; }
            //    if (DtmValue != null) { value = (T)(object)DtmValue; return true; }
            //    if (StrValue != null) { value = (T)(object)StrValue; return true; }
            //    throw new InvalidTypeException(
            //        "Unable to find object.\r\n" +
            //        "Details:\r\n" + ToString());
            //}

            //if (typeof(long).Equals(t)) {
            //    if (LngValue != null) { value = (T)(_ADotTypeValue = LngValue); return true; };
            //    value = default(T); return false;
            //}
            //if (typeof(int).Equals(t)) throw new TypeIntNotSupportedByAgoRapideException(ToString());

            //if (typeof(double).Equals(t)) {
            //    if (DblValue != null) { value = (T)(_ADotTypeValue = DblValue); return true; };
            //    value = default(T); return false;
            //}
            //if (typeof(bool).Equals(t)) {
            //    if (BlnValue != null) { value = (T)(_ADotTypeValue = BlnValue); return true; };
            //    value = default(T); return false;
            //}
            //if (typeof(DateTime).Equals(t)) {
            //    if (DtmValue != null) { value = (T)(_ADotTypeValue = DtmValue); return true; };
            //    value = default(T); return false;
            //}
            //if (typeof(string).Equals(t)) {
            //    // TODO: CLEAN UP THIS. WHAT TO RETURN NOW? 
            //    //if (StrValue != null) return (T)(object)StrValue;

            //    //// TODO: DECIDE WHAT TO USE. String representation found in Initialize or in TryGetV
            //    // TODO: THIS IS NOT GOOD. The ToString-representation is not very helpful in a lot of cases (Type, DateTime and so on(
            //    //if (_ADotTypeValue != null) { value = (T)(object)_ADotTypeValue.ToString(); return true; };

            //    /// TODO: WHAT IS THE MEANING OF SETTING <see cref="_ADotTypeValue"/> now?
            //    /// TODO: WHAT IS THE PURPOSE OF SETTING IT TO A STRING? A STRING IS NOT WHAT IT IS SUPPOSED TO BE?
            //    if (LngValue != null) { value = (T)(_ADotTypeValue = LngValue.ToString()); return true; }; /// TODO: Introduce a <see cref="AgoRapideAttribute"/>.LngFormat property here
            //    if (DblValue != null) { value = (T)(_ADotTypeValue = ((double)DblValue).ToString2()); return true; }; /// TODO: Introduce a <see cref="AgoRapideAttribute"/>.DblFormat property here
            //    if (DtmValue != null) { value = (T)(_ADotTypeValue = ((DateTime)DtmValue).ToString(Key.Key.A.DateTimeFormat)); return true; };
            //    if (BlnValue != null) { value = (T)(_ADotTypeValue = BlnValue.ToString()); return true; }; // TODO: Better ToString here!
            //    if (GeoValue != null) { value = (T)(_ADotTypeValue = GeoValue); return true; };
            //    if (StrValue != null) { value = (T)(_ADotTypeValue = StrValue); return true; };

            //    if (_ADotTypeValue != null) { // TODO: Is this code relevant? Should not the specific value have been set anyway?
            //        // The ToString-representation is not very helpful in some cases (Type, DateTime and so on
            //        // Therefore we check for those first
            //        if (_ADotTypeValue is double) { value = (T)(object)((double)_ADotTypeValue).ToString2(); return true; }
            //        if (_ADotTypeValue is DateTime) { value = (T)(object)((DateTime)_ADotTypeValue).ToString(DateTimeFormat.DateHourMin); return true; }
            //        // For all others we accept the default ToString conversion.
            //        value = (T)(object)_ADotTypeValue.ToString(); return true;
            //    };

            //    // TODO: REPLACE WITH KIND OF "NO KNOWN TYPE OF PROPERTY VALUE FOUND"                
            //    throw new InvalidPropertyException("Unable to find string value. Details: " + ToString());
            //    // Do not return default(T) because we should always be able to convert a property to string
            //    // value = default(T); return false;                
            //}
            //if (typeof(Type).Equals(t)) {
            //    // TODO: Move typeof(Type).Equals(t) into AgoRapideAttribute.TryValidateAndParse instead. 
            //    if (Util.TryGetTypeFromString(StrValue, out var temp)) { value = (T)(_ADotTypeValue = temp); return true; }
            //    value = default(T); return false;
            //}

            //if (typeof(Uri).Equals(t)) {
            //    // TODO: Move typeof(Uri).Equals(t) into AgoRapideAttribute.TryValidateAndParse instead.                 
            //    if (Uri.TryCreate(StrValue, UriKind.RelativeOrAbsolute, out var temp)) { value = (T)(_ADotTypeValue = temp); return true; }
            //    value = default(T); return false;
            //}

            //if (StrValue != null) {
            //    // TODO: Try to MAKE A.TryValidateAndParse GENERIC in order for it to return a more strongly typed result.
            //    // TODO: Use that again to change its result-mechanism, not returning a property.
            //    if (Key.Key.TryValidateAndParse(StrValue, out var result)) {
            //        /// Note that TryValidateAndParse returns TRUE if no ValidatorAndParser is available
            //        /// TODO: This is not good enough. FALSE would be a better result (unless TProperty is string)
            //        /// TODO: because result.Result will now be set to a String value
            //        /// TODO: Implement some kind of test here and clean up the whole mechanism
            //        if (result.Result.StrValue != null) {
            //            throw new InvalidPropertyException(
            //                "Unable to cast '" + StrValue + "' to " + t + ", " +
            //                "ended up with " + result.Result.StrValue.GetType() + ".\r\n" +
            //                (Key.Key.ValidatorAndParser != null ?
            //                    "Very unexpected since " + nameof(AgoRapideAttributeEnriched.ValidatorAndParser) + " was set" :
            //                    "Most probably because " + nameof(AgoRapideAttributeEnriched.ValidatorAndParser) + " was not set"
            //                ) + ".\r\n" +
            //                "Details: " + ToString());
            //        }
            //        if (!(result.Result.ADotTypeValue() is T)) throw new InvalidPropertyException(
            //            "Unable to cast '" + StrValue + "' to " + t + ", " +
            //            "ended up with " + result.Result.ADotTypeValue().GetType() + " (value: '" + result.Result.ADotTypeValue().ToString() + ").\r\n" +
            //            (Key.Key.ValidatorAndParser == null ?
            //                "Very unexpected since " + nameof(AgoRapideAttributeEnriched.ValidatorAndParser) + " was not set" :
            //                "Most probably because " + nameof(AgoRapideAttributeEnriched.ValidatorAndParser) + " returns the wrong type of object"
            //            ) + ".\r\n" +
            //            "Details: " + ToString());
            //        value = (T)(_ADotTypeValue = result.Result.ADotTypeValue()); return true;
            //    }
            //    // TODO: Move typeof(Type).Equals(t) into AgoRapideAttribute.TryValidateAndParse instead. 
            //    value = default(T); return false;
            //}

            //// This code for enum-parsing does not belong here but in the A.TryValidateAndParse above
            ////if (A.A.Type != null && t.Equals(A.A.Type)) {
            ////    if (A.A.Type.IsEnum) {
            ////        Util.EnumTryParse(A.A.Type, Value, out _ADotTypeValue);
            ////        value = _ADotTypeValue == null ? default(T) : (T)_ADotTypeValue;
            ////        return _ADotTypeValue != null;
            ////    } else {
            ////        throw new NotImplementedException("T: " + typeof(T).ToString() + ", A.A.Type: " + A.A.Type?.ToString() ?? "[NULL]");
            ////    }
            ////}
            throw new NotImplementedException("T: " + typeof(T).ToString() + ", A.Type: " + (Key.Key.A.Type?.ToString() ?? "[NULL]") + ". Details:" + ToString());

            // TODO: Decide how to implement different types. Exception or not?
        }

        private AgoRapideAttribute _valueAttribute;
        /// <summary>
        /// Returns attributes for the value itself. 
        /// Usually used for giving helptext for the value. 
        /// </summary>
        public AgoRapideAttribute ValueA => _valueAttribute ?? (_valueAttribute = new Func<AgoRapideAttribute>(() => {
            if (_value == null) throw new NullReferenceException(nameof(_value) + ". Details. " + ToString());
            if (_value.GetType().IsEnum) return _value.GetAgoRapideAttribute();
            var t = _value as Type;
            if (t != null) return t.GetAgoRapideAttributeForClass();
            return DefaultAgoRapideAttribute;
        })());

        /// <summary>
        /// Use with caution. Note how the same instance is returned always. 
        /// Therefore the requester should not change this instance after "receiving" it. 
        /// </summary>
        public static AgoRapideAttribute DefaultAgoRapideAttribute = AgoRapideAttribute.GetNewDefaultInstance();

        ///// <summary>
        ///// Will validate according to attributes defined
        ///// Returns itself for fluent purposes
        ///// </summary>
        ///// <returns></returns>
        //public Property Initialize() {
        //    new Action(() => {
        //        // TODO: DECIDE WHAT TO USE. String representation found in Initialize or in TryGetV
        //        if (LngValue != null) { Value = LngValue.ToString(); return; }
        //        if (DblValue != null) { Value = ((double)DblValue).ToString2(); return; }
        //        if (BlnValue != null) { Value = ((bool)BlnValue).ToString(); return; }
        //        if (DtmValue != null) { Value = ((DateTime)DtmValue).ToString(Key.Key.A.DateTimeFormat); return; }
        //        if (StrValue != null) { Value = StrValue; return; }
        //        if (_ADotTypeValue != null) { Value = _ADotTypeValue.ToString(); return; } // TODO: Better ToString here!
        //        if (true.Equals(Key.Key.A.IsStrict)) {
        //            // TODO: Try to MAKE A.TryValidateAndParse GENERIC in order for it to return a more strongly typed result.
        //            if (!Key.Key.TryValidateAndParse(Value, out var parseResult)) throw new InvalidPropertyException(parseResult.ErrorResponse + ". Details: " + ToString());
        //            // TODO: This is difficult. Result._ADotTypeValue is most probably not set
        //            _ADotTypeValue = parseResult.Result._ADotTypeValue;
        //            // TODOk: FIX!!!
        //        }
        //        // We could parse at once, but it might never be needed so it is better to let TryGetV do it later
        //        // else if (A.Type == null && A.Type.IsEnum) {
        //        //    if (A.TryValidateAndParse(Value, out var parsedValue, out _)) _ADotTypeValue = parsedValue;
        //        //}
        //        // TODO: REPLACE WITH KIND OF "NO KNOWN TYPE OF PROPERTY VALUE FOUND"
        //        throw new InvalidPropertyException("Unable to find string value for " + ToString());
        //    })();
        //    if (Key.Key.ValidatorAndParser != null) {
        //        // We could consider running the validator now if it was not already run, 
        //        // but it would be quite meaningless
        //        // if it is a standard TryParse for Long for instance, because LngValue is already set

        //        // TODO: Consider distinguishing between SyntactivalValidator and RangeValidator
        //        // TODO: In other words, validate for range 1-10 (after we have checked that is a long)
        //    }
        //    return this;
        //}

        /// <summary>
        /// Note existence of both <see cref="Property.InvalidPropertyException"/> and <see cref="BaseEntity.InvalidPropertyException{T}"/>
        /// </summary>
        private class InvalidPropertyException : ApplicationException {
            public InvalidPropertyException(string message) : base(message) { }
        }

        /// <summary>
        /// Note: This method must be failsafe since it provides debug information.
        /// Note: Make sure it never introduces any recursivity or chance for exceptions being thrown.
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            base.ToString() + "\r\n" +
            (IsIsManyParent ? nameof(IsIsManyParent) : "") +
            (IsTemplateOnly ? nameof(IsTemplateOnly) : "") +
            nameof(ParentId) + ": " + ParentId + ", " +
            nameof(KeyDB) + ": " + (_keyDB ?? "[NULL]") + ", " +
            nameof(Key.Key.CoreP) + ": " + (_key?.Key.PExplained ?? "[NULL]") + ", " +
            nameof(_stringValue) + ": " + (_stringValue ?? "[NULL]") + ", " +
            //nameof(LngValue) + ": " + (LngValue?.ToString() ?? "[NULL]") + ", " +
            //nameof(DblValue) + ": " + (DblValue?.ToString() ?? "[NULL]") + ", " +
            //nameof(BlnValue) + ": " + (BlnValue?.ToString() ?? "[NULL]") + ", " +
            //nameof(DtmValue) + ": " + (DtmValue?.ToString() ?? "[NULL]") + ", " +
            //nameof(GeoValue) + ": " + (GeoValue?.ToString() ?? "[NULL]") + ", " +
            //nameof(StrValue) + ": " + (StrValue ?? "[NULL]") +
            (_key == null ? "" : (", " + nameof(Key.Key.A.Type) + ": " + (_key.Key.A.Type?.ToString() ?? "[NULL]"))) + ", " +
            GetType() + ".\r\n";

        public override string ToHTMLTableHeading(Request request) => HTMLTableHeading;
        public const string HTMLTableHeading = "<tr><th>Key</th><th>Value</th><th>Save</th><th>" + nameof(Created) + "</th><th>" + nameof(Invalid) + "</th></tr>";

        /// <summary>
        /// Hack for transferring information from 
        /// <see cref="BaseEntity.CreateHTMLForExistingProperties"/> and 
        /// <see cref="BaseEntity.CreateHTMLForAddingProperties"/> to 
        /// <see cref="ToHTMLTableRow"/>. 
        /// Do not use except between these methods. 
        /// Hack implemented because of difficulty of adding parameter to <see cref="ToHTMLTableRow"/>. 
        /// </summary>
        public bool IsChangeableByCurrentUser;

        /// <summary>
        /// TODO: Create better links, use <see cref="CoreMethod"/> or similar in order to get the REAL URL's used by the actual methods.
        /// 
        /// Note that may return multiple rows if <see cref="IsIsManyParent"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public override string ToHTMLTableRow(Request request) {
            if (IsIsManyParent) return string.Join("\r\n", Properties.Select(p => {
                p.Value.IsChangeableByCurrentUser = IsChangeableByCurrentUser;
                return p.Value.ToHTMLTableRow(request);
            }));
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
                        case DBField.key: if (!string.IsNullOrEmpty(Key.Key.A.WholeDescription)) return (Key.Key.A.WholeDescription).HTMLEncode(); return null;
                        case DBField.strv: if (!string.IsNullOrEmpty(ValueA.WholeDescription)) return (ValueA.WholeDescription).HTMLEncode(); return null;
                        default: return null;
                    }
                })();

                retval.AppendLine("<tr><td>" +
                    (string.IsNullOrEmpty(A.A.WholeDescription) ? "" : "<span title=\"" + (A.A.WholeDescription).HTMLEncode() + "\">") +
                    field.ToString() +
                    (string.IsNullOrEmpty(A.A.WholeDescription) ? "" : " (+)</span>") +
                    "</td><td>" +
                    (includeDescription == null ? "" : "<span title=\"" + includeDescription + "\">") +
                    (value?.HTMLEncodeAndEnrich(request) ?? "&nbsp;") +
                    (includeDescription == null ? "" : " (+)</span>") +
                    "</td></tr>");
            });
            var adderWithLink = new Action<DBField, long?>((field, value) => {
                var A = field.GetAgoRapideAttributeT();
                retval.AppendLine("<tr><td>" +
                    (string.IsNullOrEmpty(A.A.WholeDescription) ? "" : "<span title=\"" + (A.A.WholeDescription).HTMLEncode() + "\">") +
                    field.ToString() +
                    (string.IsNullOrEmpty(A.A.WholeDescription) ? "" : " (+)</span>") +
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
            retval.AppendLine("<tr><td>Index</td><td>" + (Key.Key.A.IsMany ? Key.Index.ToString() : "&nbsp;") + "</td></tr>\r\n");

            /// TODO: Maybe keep information about from which <see cref="DBField"/> <see cref="_stringValue"/> originated?
            retval.AppendLine("<tr><td>Value</td><td>" + (_stringValue?.HTMLEncode() ?? "[NULL[]") + "</td></tr>\r\n");

            //adder(DBField.lngv, LngValue?.ToString());
            //adder(DBField.dblv, DblValue?.ToString());
            //adder(DBField.blnv, BlnValue?.ToString());
            //adder(DBField.dtmv, DtmValue?.ToString());
            //adder(DBField.geov, GeoValue?.ToString());
            //adder(DBField.strv, StrValue?.ToString());

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
        /// For example of override see <see cref="BaseEntityWithLogAndCount.ToJSONEntity"/> or <see cref="Property.ToJSONEntity"/>
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
    }

    /// <summary>
    /// JSONProperty0/1/2/3/4 contains gradually more and more information
    /// </summary>
    public class JSONProperty0 : JSONEntity0 {
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

