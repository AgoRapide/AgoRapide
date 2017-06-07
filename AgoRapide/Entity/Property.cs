// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;
using AgoRapide.Database; // Used by XML-links

namespace AgoRapide {

    /// <summary>
    /// See also generic sub class <see cref="PropertyT{T}"/> 
    /// 
    /// Properties are created and validated through the following paths:
    /// 
    /// 1) Creation in database (<see cref="BaseDatabase.CreateProperty"/>)
    ///    Note that this does not involve the <see cref="Property"/>-class at all.
    /// 
    /// 2) When originating from database (<see cref="BaseDatabase.TryGetPropertyById"/>): 
    /// 
    ///    Through <see cref="Property.Create"/> which 
    ///    
    ///    either 
    ///    
    ///    a) Calls <see cref="PropertyT{T}.PropertyT"/>) directly 
    ///       (which happens whenever value itself does not originate from <see cref="DBField.strv"/> 
    ///       or when <see cref="PropertyKeyAttribute.Type"/> is <see cref="string"/>)
    ///       
    ///    or
    ///    
    ///    b) Calls <see cref="PropertyKeyAttributeEnriched.TryValidateAndParse"/> / <see cref="ParseResult.Create{T}"/> 
    ///       which again calls <see cref="PropertyT{T}.PropertyT"/>
    ///    
    /// 3) When received by API (<see cref="BaseController.TryGetRequest"/>):
    /// 
    ///    Through <see cref="PropertyKeyAttributeEnriched.TryValidateAndParse"/> 
    ///    which calls <see cref="ParseResult.Create{T}"/> 
    ///    which again calls <see cref="PropertyT{T}.PropertyT"/>) 
    ///    
    /// 4) Directly in C# code, ordinary properties (<see cref="BaseEntity.AddProperty{T}"/>
    /// 
    ///    Through direct call to <see cref="PropertyT{T}.PropertyT"/>
    ///    
    /// 5) Template and <see cref="PropertyKeyAttribute.IsMany"/> parent.
    /// 
    ///    Through <see cref="Property.CreateTemplate"/> and <see cref="Property.CreateIsManyParent"/>
    ///    TODO: This is not considered an optimal solution as of Apr 2017
    ///    TODO: Make a class called PropertyTemplate instead of using <see cref="IsTemplateOnly" />
    ///    TODO: Make a class called PropertyIsManyParent instead of using <see cref="IsIsManyParent" />
    /// 
    /// Subclass: <see cref="PropertyT{T}"/>
    /// 
    /// This class is deliberately not made abstract in order to faciliate use of "where T: new()" constraint in method signatures like
    /// <see cref="BaseDatabase.TryGetEntities{T}"/> 
    /// </summary>
    [Class(
        Description = "Represents a single property of a -" + nameof(BaseEntity) + "-.",
        LongDescription =
            "Note how -" + nameof(Property) + "- is itself also a -" + nameof(BaseEntity) + "- and may therefore contain " +
            "a collection of -" + nameof(Property) + "- itself, either because it \"is\" -" + nameof(PropertyKeyAttribute.IsMany) + "- or " +
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
        public override string IdFriendly => KeyDB;

        private string _keyDB;
        /// <summary>
        /// Key as stored in database
        /// </summary>
        public string KeyDB {
            get => _keyDB ?? (_keyDB = _key?.ToString() ?? throw new NullReferenceException(nameof(Key) + ". Either " + nameof(Key) + " or " + nameof(_keyDB) + " must be set from 'outside'"));
            set => _keyDB = value;
        }

        protected PropertyKeyWithIndex _key;
        public PropertyKeyWithIndex Key => _key ?? (_key = PropertyKeyWithIndex.Parse(_keyDB ?? throw new NullReferenceException(nameof(_keyDB) + ". Either " + nameof(_key) + " or " + nameof(_keyDB) + " must be set from 'outside'"), () => ToString()));
        /// <summary>
        /// Key for use in HTML-code (as identifiers for use by Javascript)
        /// (that is, NOT key in HTML-format)
        /// 
        /// TODO: Make id more specific because saving for instance will now fail 
        /// TODO: for multiple properties on same HTML page with same <see cref="KeyDB"/>
        /// </summary>
        public string KeyHTML => KeyDB.ToString().Replace("#", "_");

        /// <summary>
        /// Improves on <see cref="ParseResult.Result"/>
        /// 
        /// HACK: Solves problem of <see cref="PropertyKeyAttributeEnriched.TryValidateAndParse"/> / <see cref="ParseResult.Create"/> 
        /// HACK: only being aware of <see cref="PropertyKeyAttributeEnriched"/>, 
        /// HACK: not <see cref="PropertyKeyWithIndex"/> 
        /// HACK: when generating <see cref="ParseResult.Result"/>
        /// </summary>
        /// <param name="key"></param>
        public void SetKey(PropertyKeyWithIndex key) {
            _key = key;
            _keyDB = null;
        }

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
        /// Note that could theoretically be two choices for parent, either the parent entity (chosen variant) or the entity root property (<see cref="CoreP.RootProperty"/>)
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
        /// Only relevant when corresponding <see cref="PropertyKeyAttribute.IsMany"/> for <see cref="Key"/>
        /// </summary>
        public bool IsIsManyParent { get; private set; }
        public void AssertIsManyParent() {
            if (!true.Equals(IsIsManyParent)) throw new PropertyKeyAttribute.IsManyException("!" + nameof(IsIsManyParent) + ": " + ToString());
        }

        /// <summary>
        /// Example: If PhoneNumber#1 and PhoneNumber#2 exists then PhoneNumber#3 will be returned. 
        /// 
        /// TODO: Make a class called PropertyIsManyParent instead of using <see cref="IsIsManyParent" />
        /// </summary>
        /// <returns></returns>
        public PropertyKeyWithIndex GetNextIsManyId() {
            AssertIsManyParent();
            var id = 1; while (Properties.ContainsKey((CoreP)(object)(int.MaxValue - id))) {
                id++; if (id > 1000) throw new PropertyKeyAttribute.IsManyException("id " + id + ", limit is (somewhat artificially) set to 1000. " + ToString());
            }
            return new PropertyKeyWithIndex(Key.Key, id);
        }

        /// <summary>
        /// TODO: Make a class called PropertyIsManyParent instead of using <see cref="IsIsManyParent" />
        /// </summary>
        /// <param name="key"></param>
        /// <param name="property"></param>
        public void AddPropertyForIsManyParent(CoreP key, Property property) {
            AssertIsManyParent();
            Properties.AddValue2(key, property);
            _value = null; /// Important since <see cref="TryGetV{T}"/> caches last value found for a given type
        }

        /// <summary>
        /// Do not use this constructor. It always throws an exception.
        /// </summary>
        public Property() => throw new InvalidPropertyException(
            "Do not use this constructor. " +
            "This parameterless public constructor only exists in order to satisfy restrictions in " +
            nameof(BaseDatabase) + " like \"where T : BaseEntity, new()\" for " +
            nameof(BaseDatabase.TryGetEntities) + " and " +
            nameof(BaseDatabase.TryGetEntityById) + ". " +
            "(None of these actually use this constructor anyway because they redirect to " + nameof(BaseDatabase.TryGetPropertyById) + " when relevant)");

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
        public static Property CreateTemplate(PropertyKeyWithIndex key, BaseEntity parent) => new Property(dummy: null) {
            IsTemplateOnly = true,
            _key = key,
            Parent = parent,
            ParentId = parent.Id
        };

        /// <summary>
        /// See <see cref="IsIsManyParent"/> for documentation. 
        /// TODO: Make a class called PropertyIsManyParent instead of using <see cref="IsIsManyParent" />
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Property CreateIsManyParent(PropertyKey key) {
            var strictKey = key.PropertyKeyIsSet ?
                key.PropertyKeyAsIsManyParentOrTemplate :
                /// Above is not sufficient for instance when <param name="key"/> does not originate from EnumMapper.
                /// Therefore we must to this instead:
                PropertyKeyMapper.GetA(key.Key.CoreP).PropertyKeyAsIsManyParentOrTemplate;
            // TODO: Code above is a bit slow performance wise (there are two dictionary look ups involved)
            /// TODO: Code above is run whenever an <see cref="PropertyKeyAttribute.IsMany"/> property is read from database for instance.

            return new Property(dummy: null) {
                IsIsManyParent = true,
                _key = strictKey,
                Properties = new Dictionary<CoreP, Property>()
            };
        }

        /// <summary>
        /// Used when reading from database.
        /// 
        /// Note how internally creates a <see cref="PropertyT{T}"/> object. 
        /// </summary>
        /// <returns></returns>
        public static Property Create(
            PropertyKeyWithIndex key,
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
                if (lngValue != null) return new PropertyT<long>(key, (long)lngValue, lngValue.ToString(), valueAttribute: null);
                if (dblValue != null) return new PropertyT<double>(key, (double)dblValue, ((double)dblValue).ToString2(), valueAttribute: null);
                if (blnValue != null) return new PropertyT<bool>(key, (bool)blnValue, ((bool)blnValue).ToString(), valueAttribute: null);
                if (dtmValue != null) return new PropertyT<DateTime>(key, (DateTime)dtmValue, ((DateTime)dtmValue).ToString(key.Key.A.DateTimeFormat), valueAttribute: null);
                if (strValue == null) throw new NullReferenceException("None of the following was set: " + nameof(lngValue) + ", " + nameof(dblValue) + ", " + nameof(blnValue) + ", " + nameof(dtmValue) + ", " + nameof(strValue) + ".\r\nDetails: " + key.Key.ToString());
                if (key.Key.A.Type.Equals(typeof(string))) return new PropertyT<string>(key, strValue, strValue, valueAttribute: null);
                if (!key.Key.TryValidateAndParse(strValue, out var parseResult)) {
                    if (key.Key.A.IsNotStrict) return new PropertyT<string>(key, strValue, strValue, valueAttribute: null);
                    throw new InvalidPropertyException(
                        parseResult.ErrorResponse + ".\r\n" +
                        "Possible resolution:\r\n" +
                        "  DELETE FROM p WHERE " + DBField.id + " = " + id + "\r\n" +
                        "Details: " + key.Key.ToString());
                }
                var r = parseResult.Result;
                r.SetKey(key); // HACK
                return r;
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
        public object Value => _value ?? throw new NullReferenceException(nameof(_value) + ".\r\nDetails: " + ToString());

        private string _valueHTML;
        /// <summary>
        /// TODO: Add support for both <see cref="ValueHTML"/> AND <see cref="IsChangeableByCurrentUser"/>"/>
        /// 
        /// Calls <see cref="Documentator.ReplaceKeys"/> as necessary.
        /// 
        /// Note how result in itself is cached, something which is very useful when <see cref="Parent"/> itself is cached 
        /// since the process of inserting links is quite performance heavy.
        /// </summary>
        /// <returns></returns>
        public string ValueHTML => _valueHTML ?? (_valueHTML = new Func<string>(() => {  // => _valueHTMLCache.GetOrAdd(request.ResponseFormat, dummy => {
            switch (Key.Key.CoreP) {
                case CoreP.QueryId: {
                        var v = V<QueryId>();
                        return APICommandCreator.HTMLInstance.CreateAPILink(CoreAPIMethod.EntityIndex, v.ToString(), (Parent != null ? Parent.GetType() : typeof(BaseEntity)), v);
                    }
                case CoreP.DBId: {
                        var v = V<string>();
                        return APICommandCreator.HTMLInstance.CreateAPILink(CoreAPIMethod.EntityIndex, v,
                           (Parent != null && APIMethod.TryGetByCoreMethodAndEntityType(CoreAPIMethod.EntityIndex, Parent.GetType(), out _) ?
                                Parent.GetType() : /// Note how parent may be <see cref="Result"/> or similar in which case no <see cref="APIMethod"/> exists, therefore the APIMethod.TryGetByCoreMethodAndEntityType test. 
                            typeof(BaseEntity)
                           ), new QueryIdInteger(V<long>()));
                    }
                default: {
                        var v = V<string>();
                        if (Key.Key.A.IsDocumentation) {
                            return Documentator.ReplaceKeys(v.HTMLEncode());
                        } else if (!ValueA.IsDefault && Documentator.Keys.TryGetValue(v, out var list)) {
                            return Documentator.GetSingleReplacement(v, list);
                        } else {
                            return v.HTMLEncodeAndEnrich(APICommandCreator.HTMLInstance);
                        }
                    }
            }
        })());

        public T V<T>() => TryGetV(out T retval) ? retval : throw new InvalidPropertyException("Unable to convert value '" + _stringValue + "' to " + typeof(T).ToString() + ", A.Type: " + (Key.Key.A.Type?.ToString() ?? "[NULL]") + ". Was the Property-object correct initialized? Details: " + ToString());
        /// <summary>
        /// TODO: Decide what "Try" really means. 
        /// TODO: Consider never returning false, that is, rename this method into V instead and stop using the "Try"-concept. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetV<T>(out T value) {
            if (_value != null && _value is T) { value = (T)_value; return true; } // Note how "as T" is not possible to use here
            var t = typeof(T);

            if (IsIsManyParent) {
                if (typeof(string).Equals(t)) {
                    value = (T)(_value = string.Join(", ", Properties.Select(p => p.Value.V<string>()))); // Note caching in _value
                    return true;
                }
                if (typeof(List<string>).Equals(t)) {
                    value = (T)(_value = Properties.Select(p => p.Value.V<string>()).ToList()); // Note caching in _value
                    return true;
                }
                if (typeof(List<object>).Equals(t)) {
                    value = (T)(_value = Properties.Select(p => p.Value.Value).ToList()); // Note caching in _value
                    return true;
                }
                if (t.IsGenericType) {
                    InvalidTypeException.AssertList(t, Key, () => ToString());
                    var iList = (System.Collections.IList)System.Activator.CreateInstance(t);
                    Properties.ForEach(p => {
                        // InvalidTypeException.AssertAssignable(p.Value.Value.GetType(), Key.Key.A.Type, () => p.ToString());
                        iList.Add(p.Value.Value);
                    });
                    _value = iList; // Note caching in _value
                    value = (T)_value;
                    return true;
                }
            } else {
                if (typeof(string).Equals(t)) {
                    value = (T)(object)(_stringValue ?? throw new NullReferenceException(nameof(_stringValue) + ". Details: " + ToString()));
                    return true;
                }
            }

            /// TODO: Consider never returning false, that is, rename this method into V instead and stop using the "Try"-concept. 
            value = default(T);
            return false;
            // throw new NotImplementedException("T: " + typeof(T).ToString() + ", A.Type: " + (Key.Key.A.Type?.ToString() ?? "[NULL]") + ". Details:" + ToString());
            // TODO: Decide how to implement different types. Exception or not?
        }

        private BaseAttribute _valueA;
        /// <summary>
        /// Returns attributes for <see cref="Value"/>. 
        /// Should only be used for giving helptext for the value. 
        /// </summary>
        public BaseAttribute ValueA => _valueA ?? (_valueA = new Func<BaseAttribute>(() => {
            if (_value == null) throw new NullReferenceException(nameof(_value) + ". Details. " + ToString());
            switch (_value) {
                case ApplicationPart applicationPart: return applicationPart.A;                    
            }
            var type = _value.GetType();
            if (type.IsEnum) return _value.GetEnumValueAttribute();
            
            // NOT RELEVANT
            // if (type.IsClass) return type.GetClassAttribute(); // TODO: Check validity of this

            // TOOD: Add some more code here!

            // TODO: Remove comment. Old attempt.
            //if (typeof(ITypeDescriber).IsAssignableFrom(type)) {
            //    /// TODO: Add check for <see cref="ClassAttribute"/> here? 
            //    /// Especially for <see cref="ITypeDescriber"/>
            //    throw new NotImplementedException();
            //}
            return DefaultAgoRapideAttribute;
        })());

        /// <summary>
        /// Use with caution. Note how the same instance is returned always. 
        /// Therefore the requester should not change this instance after "receiving" it. 
        /// </summary>
        private static BaseAttribute DefaultAgoRapideAttribute = BaseAttribute.GetStaticNotToBeUsedInstance;

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
            nameof(Key.Key.CoreP) + ": " + (_key?.Key.A.EnumValueExplained ?? "[NULL]") + ", " +
            nameof(_stringValue) + ": " + (_stringValue ?? "[NULL]") + ", " +
            (_key == null ? "" : (", " + nameof(Key.Key.A.Type) + ": " + (_key.Key.A.Type?.ToString() ?? "[NULL]"))) + ", " +
            GetType() + ".\r\n";

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
        /// Consider removing <paramref name="request"/> from <see cref="BaseEntity.ToHTMLTableRowHeading"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public override string ToHTMLTableRowHeading(Request request) => HTMLTableHeading;
        public const string HTMLTableHeading = "<tr><th>Key</th><th>Value</th><th>Save</th><th>" + nameof(Created) + "</th><th>" + nameof(Invalid) + "</th></tr>";

        /// <summary>
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
            return "<tr><td>" + /// TODO: Use StringBuilder. Makes for more efficient code and also code that is easier to debug.

                // --------------------
                // Column 1, Key
                // --------------------
                (Id <= 0 ? IdFriendly.HTMLEncode() : request.API.CreateAPILink(this)).HTMLEncloseWithinTooltip(a.Description) +
                "</td><td>" +

                // --------------------
                // Column 2, Value
                // --------------------
                ((!IsChangeableByCurrentUser || a.ValidValues != null) ?
                    // Note how passwords are not shown (although they are stored salted and hashed and therefore kind of "protected" we still do not want to show them)
                    (a.IsPassword ? "[SET]" : (IsTemplateOnly ? "" : ValueHTML)) : /// TODO: Add support for both <see cref="ValueHTML"/> AND <see cref="IsChangeableByCurrentUser"/>"/>
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
                ).HTMLEncloseWithinTooltip(IsTemplateOnly ? "" : ValueA.Description) +
                "</td><td>" +

                // --------------------
                // Column 3, Save button or SELECT
                // --------------------
                (!IsChangeableByCurrentUser || Parent == null ? "&nbsp;" : /// Note check for <see cref="Parent"/>, we need that for calling API
                    (a.ValidValues == null ?
                        (
                            // Ordinary textbox was presented. Add button.

                            /// Note: Corresponding Javascript method being called here is currently generated in <see cref="HTMLView.GetHTMLStart"/>

                            /// TODO: An alternative to the above would be to 
                            /// TODO: consider making <see cref="APIMethod"/> create Javascript such as this automatically...
                            /// TODO: In other words, call the <see cref="APIMethod"/> for <see cref="CoreAPIMethod.UpdateProperty"/> 
                            /// TODO: in order to get the Javascript required here, instead of generating it as done immediately below:
                            "<input " +
                                "type=\"button\" " +
                                "value = \"Save\" " +
                                "onclick = \"try { " +
                                        CoreAPIMethod.UpdateProperty + "('" + KeyHTML + "', '" + Parent.GetType().ToStringVeryShort() + "', '" + ParentId + "', '" + KeyDB + "'); " +
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
                                        CoreAPIMethod.UpdateProperty + "('" + KeyHTML + "', '" + Parent.GetType().ToStringVeryShort() + "', '" + ParentId + "', '" + KeyDB + "'); " +
                                    "} catch (err) { " +
                                        "com.AgoRapide.AgoRapide.log(err); " +
                                    "} return false;" +
                                "\"" +
                            "/>" +
                            /// TODO: Idea for <see cref="Property.ToHTMLTableRow(Request)"/>
                            /// TODO: SELECT values for choosing should also have PropertyOperation in them, se we can immediately
                            /// TODO: delete properties from the HTML admin interface.
                            /// TOOD: (but that would leave properties without <see cref="PropertyKeyAttribute.ValidValues"/> without such...)
                            /// TODO: Maybe better to just leave as is...

                            "<option value=\"\">[Choose " + IdFriendly.HTMLEncode() + "...]</option>\r\n" +
                            /// TODO: Add to <see cref="PropertyKeyAttribute.ValidValues"/> a List of tuples with description for each value
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
                var includeDescription = new Func<string>(() => {
                    switch (field) {
                        case DBField.key: if (!string.IsNullOrEmpty(Key.Key.A.WholeDescription)) return (Key.Key.A.WholeDescription).HTMLEncode(); return null;
                        case DBField.strv: if (!string.IsNullOrEmpty(ValueA.WholeDescription)) return (ValueA.WholeDescription).HTMLEncode(); return null;
                        default: return null;
                    }
                })();

                retval.AppendLine("<tr><td>" +
                    field.ToString().HTMLEncloseWithinTooltip(field.A().Key.A.WholeDescription) +
                    "</td><td>" +
                    (value?.HTMLEncode() ?? "&nbsp;").HTMLEncloseWithinTooltip(includeDescription) +
                    "</td></tr>");
            });
            var adderWithLink = new Action<DBField, long?>((field, value) => {
                retval.AppendLine("<tr><td>" +
                    field.ToString().HTMLEncloseWithinTooltip(field.A().Key.A.WholeDescription) +
                    "</td><td>" +
                    (value != null && value != 0 ?
                        (InMemoryCache.EntityCache.TryGetValue((long)value, out var entity) ?
                            request.API.CreateAPILink(entity) :
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

            // TODO: Add helptext for this (or remove it).
            retval.AppendLine("<tr><td>Index</td><td>" + (Key.Key.A.IsMany ? Key.Index.ToString() : "&nbsp;") + "</td></tr>\r\n");

            /// TODO: Maybe keep information about from which <see cref="DBField"/> <see cref="_stringValue"/> originated?
            retval.AppendLine("<tr><td>Value</td><td>" + (_stringValue != null ? Value : "[NULL[]") + "</td></tr>\r\n");

            adder(DBField.valid, Valid?.ToString(DateTimeFormat.DateHourMinSec));
            adderWithLink(DBField.vid, ValidatorId);
            adder(DBField.invalid, Invalid?.ToString(DateTimeFormat.DateHourMinSec));
            adderWithLink(DBField.iid, InvalidatorId);
            retval.AppendLine("</table>");

            /// This is no longer needed after introduction of <see cref="Extensions.GetBaseEntityMethods(Type)"/>
            //var cmds = new List<string>();
            //request.API.CreateAPICommand(CoreAPIMethod.History, GetType(), new QueryIdInteger(Id)).Use(cmd => {
            //    retval.AppendLine("<p>" + request.API.CreateAPILink(cmd, "History") + "</p>");
            //    cmds.Add(cmd);
            //});
            //Util.EnumGetValues<PropertyOperation>().ForEach(o => {
            //    request.API.CreateAPICommand(CoreAPIMethod.PropertyOperation, GetType(), new QueryIdInteger(Id), o).Use(cmd => {
            //        retval.AppendLine("<p>" + request.API.CreateAPILink(cmd, o.ToString()) + "</p>");
            //        cmds.Add(cmd);
            //    });
            //});
            // request.Result.AddProperty(CoreP.SuggestedUrl.A(), string.Join("\r\n", cmds.Select(cmd => request.API.CreateAPIUrl(cmd))));

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
        /// Is currently <see cref="PropertyKeyAttributeEnriched.PToString"/>. Maybe change to ToStringShort or similar.
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

