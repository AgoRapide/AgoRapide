using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;

namespace AgoRapide.Core {
    /// <summary>
    /// TODO: Make this inherit <see cref="BaseEntityT"/> and store Properties to database. In this manner we
    /// TODO: get HISTORICAL information about documentation (for each and every attribute of a property), giving us much
    /// TODO: better documentation of the application.    
    /// TODO: ACTUALLY, make this inherit <see cref="ApplicationPart"/> (with its own id against database)
    /// TODO: and with its own <see cref="CoreMethod"/> called AgoRapideAttribute.
    /// 
    /// TODO: (AFTER IMPLEMENTING ABOVE) MOVE THIS TO ENTITY-MAP SINCE INHERITS <see cref="BaseEntityT"/> 
    /// 
    /// TODO: SPLIT <see cref="AgoRapideAttribute"/> into EnumAttribute and ClassAttribute.
    ///
    /// Extends on <see cref="AgoRapideAttribute"/> because that class is very limited since it is an <see cref="Attribute"/>-class
    /// 
    /// TODO: As of Jan 2017 there is still some work to be done in this class regarding parsing and validation
    /// </summary>
    public class AgoRapideAttributeT<T> where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"

        public AgoRapideAttribute A { get; private set; }

        /// <summary>
        /// TODO: UPDATE THIS COMMENT!
        /// 
        /// Corresponds normally to <see cref="AgoRapideAttribute.Property"/> (except when <see cref="P"/> is a silently mapped <see cref="CoreProperty"/>) but more strongly typed.
        /// 
        /// Normally you would use the strongly typed <see cref="AgoRapideAttributeT.P"/> instead of <see cref="AgoRapideAttribute.Property"/>
        /// </summary>
        public T P;
        public CoreProperty? _coreProperty;
        public CoreProperty CoreProperty => _coreProperty ?? (A.Property as CoreProperty? ?? CoreProperty.None);

        public string _pToString;
        /// <summary>
        /// TODO: UPDATE THIS COMMENT!
        /// 
        /// Gives either <see cref="P"/>.ToString() or <see cref="CoreProperty"/>.ToString().
        /// </summary>
        public string PToString => _pToString ?? (_pToString = typeof(T).Equals(typeof(CoreProperty)) ? A.Property.ToString() : P.ToString());

        public string _pExplained;
        /// <summary>
        /// TODO: UPDATE THIS COMMENT!
        /// 
        /// Will normally result in something like 
        ///    "P.first_name" 
        /// (if <see cref="TProperty"/> is AgoRapide.P for an enum called "first_name") 
        /// but with a more detailed explanation like 
        ///    "CoreProperty.message (mapped to P.10001)"
        /// for silently mapped <see cref="CoreProperty"/>-values.
        /// 
        /// TODO: If very high value (like almost MaxInt), then explain this as a IsMany-property where P is the index
        /// </summary>
        public string PExplained => _pExplained ?? (_pExplained = typeof(T).Equals(A.Property.GetType()) ?
            (typeof(T).ToStringVeryShort() + "." + P.ToString()) :
            (A.Property.GetType() + "." + CoreProperty + " (mapped to " + typeof(T).ToStringVeryShort() + "." + P.ToString() + ")"));

        private ConcurrentDictionary<Type, bool> _isParentForCache = new ConcurrentDictionary<Type, bool>();
        /// <summary>
        /// Explains if this property belongs to the given entity type (based on <see cref="AgoRapideAttribute.Parents"/>). 
        /// Used by <see cref="Extensions.GetObligatoryChildProperties(Type)"/>. 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsParentFor(Type type) => _isParentForCache.GetOrAdd(type, t => A.Parents != null && A.Parents.Any(p => p.IsAssignableFrom(t)));

        /// <summary>
        /// Typically used by an <see cref="IGroupDescriber"/>.
        /// Assumed to be used only at application initialization.
        /// </summary>
        /// <param name="type"></param>
        public void AddParent(Type type) {
            if (A.Parents == null) {
                A.Parents = new Type[] { type }; return;
            }
            if (A.Parents.Any(t => t.Equals(type))) return;
            var temp = A.Parents.ToList();
            temp.Add(type);
            A.Parents = temp.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        /// <param name="coreProperty">Relevant when <paramref name="agoRapideAttribute"/> is being mapped to a <see cref="CoreProperty"/></param>
        public AgoRapideAttributeT(AgoRapideAttribute agoRapideAttribute, CoreProperty? coreProperty) {
            A = agoRapideAttribute;
            /// TODO: This name is misleading. We could as well be called from <see cref="EnumMapper.MapEnum{T}"/>
            var isAttributeForCorePropertyItself = typeof(T).Equals(typeof(CoreProperty));
            if (coreProperty != null) {
                _coreProperty = coreProperty; /// This looks like an entity property enum like P. Most probably we are called from <see cref="EnumMapper.MapEnum{T}"/>. 
            } else if (isAttributeForCorePropertyItself) {
                _coreProperty = A.Property as CoreProperty? ?? throw new InvalidObjectTypeException(A.Property, typeof(CoreProperty), nameof(A.Property) + ".\r\nDetails: " + ToString());
            } else {
                _coreProperty = null; // This is quite normal. The actual enum is not an entity property enum. 
            }
            if (coreProperty != null && isAttributeForCorePropertyItself) {
                if (!(coreProperty is T)) throw new InvalidObjectTypeException(coreProperty, typeof(T), nameof(coreProperty) + ".\r\nDetails: " + ToString());
                P = (T)(object)coreProperty;
            } else {
                if (!(A.Property is T)) throw new InvalidObjectTypeException(A.Property, typeof(T), nameof(A.Property) + ".\r\nDetails: " + ToString());
                P = (T)A.Property;
            }

            /// Enrichment 1, from CoreProperty
            /// -----------------------------------------
            if (isAttributeForCorePropertyItself) {
                // Do not enrich, would result in recursive call
            } else if (A.CoreProperty == CoreProperty.None) {
                // Nothing to enrich from 
            } else {
                // Enrich from CoreProperty, this will among others give us Type which is necessary below
                A.EnrichFrom(A.CoreProperty.GetAgoRapideAttribute().A); // Careful, recursive call
            }

            /// Enrichment 2, from enum-"class" (see <see cref="CoreProperty.CoreMethod"/> / <see cref="CoreMethod"/> for example)
            /// -----------------------------------------
            if (false) { // if (isSilentlyMappedCoreProperty) {
                // REMOVE THIS!
                // Do not enrich from enum-"class" because we already have that information (since we enriched from CoreProperty above)
            } else if (A.Type == null) {
                // Nothing to enrich from 
            } else {
                var typeAttribute = A.Type.GetAgoRapideAttribute();
                if (typeAttribute.IsDefault) {
                    // Nothing interesting / nothing of value
                } else {
                    A.EnrichFrom(typeAttribute); /// Some of the properties for <see cref="AgoRapideAttribute"/> are not relevant in this case, like <see cref="IsMany"/>
                }
            }

            /// Enrichment 3, from <see cref="IGroupDescriber"/>
            /// -----------------------------------------
            var thisAsCoreProperty = this as AgoRapideAttributeT<CoreProperty>;
            if (thisAsCoreProperty != null & A.Group != null) {
                InvalidTypeException.AssertAssignable(A.Group, typeof(IGroupDescriber), () => "Type given as " + typeof(AgoRapideAttribute).ToString() + "." + nameof(AgoRapideAttribute.Group) + " to " + typeof(CoreProperty).ToString() + "." + A.Property + " must implement " + typeof(Core.IGroupDescriber));
                try {
                    ((IGroupDescriber)Activator.CreateInstance(A.Group)).EnrichAttribute(thisAsCoreProperty);
                } catch (Exception ex) {
                    throw new AgoRapideAttributeException(
                        "Unable to initialize instance of " + A.Group + " given as " + typeof(AgoRapideAttribute).ToString() + "." + nameof(AgoRapideAttribute.Group) + " to " + typeof(CoreProperty).ToString() + "." + A.Property + ".\r\n" +
                        "Most probably because " + A.Group + " does not have a default constructor without any arguments\r\n" +
                        "Details:\r\n" + agoRapideAttribute.ToString(), ex);
                }
            }

            /// Enrichment 4, from <see cref="ITypeDescriber"/>
            /// -----------------------------------------
            if (A.Type != null && typeof(ITypeDescriber).IsAssignableFrom(A.Type)) {
                var methodName = nameof(IGroupDescriber.EnrichAttribute); /// Note that <see cref="ITypeDescriber"/> itself is "empty".
                try {
                    var method = A.Type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new Type[] { GetType() }, null) ?? throw new InvalidTypeException(A.Type, "Does not have a public static method called " + methodName);
                    // Mistake Mar 2017, we thought method had to be generic:
                    // method.MakeGenericMethod(typeof(TProperty)).Invoke(null, new object[] { this });
                    method.Invoke(null, new object[] { this });
                } catch (Exception ex) {
                    throw new AgoRapideAttributeException(
                        "Unable to invoke \r\n" + A.Type.ToStringShort() + "'s\r\n" +
                        "   public static void method " + methodName + "\r\n" +
                        "given as " + typeof(AgoRapideAttribute).ToString() + "." + nameof(AgoRapideAttribute.Type) + " to " + typeof(CoreProperty).ToString() + "." + A.Property + "\r\n" +
                        "Resolution: Check that it exists and that it takes exactly one parameter of type " + GetType().ToStringShort() + ".\r\n" +
                        "In other words it should look like\r\n\r\n" +
                        "   public static void method " + methodName + "(" + GetType().ToStringShort() + " agoRapideAttribute)\r\n\r\n" +
                        "Details:\r\n" + agoRapideAttribute.ToString(), ex);
                }
            }

            /// Enrichment 5, autonomous (deduced from already known information)
            /// -----------------------------------------
            if (A.ValidValues == null && A.Type != null) {
                if (A.Type.IsEnum) {
                    A.ValidValues = Util.EnumGetValues(A.Type).ToArray();
                    if (A.SampleValues != null) {
                        throw new AgoRapideAttributeException(
                            "It is illegal (unnecessary) to combine " + nameof(A.SampleValues) + " with Type.IsEnum (" + A.Type.ToStringShort() + ") " +
                            "because " + nameof(A.ValidValues) + " can be used instead.\r\n" +
                            "Details:\r\n" + agoRapideAttribute.ToString());
                    }
                    A.SampleValues = A.ValidValues;
                } else {
                    // Difficult to think of something here
                }
            }

            if (Cleaner == null && A.Type != null) {
                if (
                    A.Type.Equals(typeof(int)) || // TODO: Can we use switch in C# 7.0 here?
                    A.Type.Equals(typeof(long)) ||
                    A.Type.Equals(typeof(string))
                    ) {
                    Cleaner = value => value?.Trim();
                } else if (A.Type.Equals(typeof(bool))) {
                    Cleaner = value => {
                        if (value == null) return null;
                        value = value.Trim();
                        switch (value) {
                            case "0":
                            case "1": return value;
                            case "false":
                            case "False":
                            case "FALSE": return "0";
                            case "true":
                            case "True":
                            case "TRUE": return "1";
                            default: break;
                        }
                        return value;
                    };
                } else {
                    Cleaner = value => value?.Trim(); // Add manual cleaner value => value if need for trim'ing.
                }
            }

            // Build validatorAndParser in chains

            if (!string.IsNullOrEmpty(A.RegExpValidator)) throw new NotImplementedException(
                typeof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.RegExpValidator) + "\r\n" +
                "Details: " + agoRapideAttribute.ToString());

            // Note how ValidatorAndParser-objects are duplicated for each and every TProperty / CoreProperty
            // This is assumed to be of little significance however as the number of such enum values is quite limited. 
            if (ValidatorAndParser != null) {
                /// OK, was most probably set through Enrichment 4, from <see cref="ITypeDescriber"/>
            } else {
                if (A.Type == null && A.ValidValues != null) {
                    // Assume type string now. 
                    // TODO: Or should we throw exception instead?
                    A.Type = typeof(string);
                }
                if (A.Type == null) {
                    ValidatorAndParser = value => {
                        throw new NotImplementedException(
                            "Validator for " + PExplained + " is not implemented because no " + nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " was given.\r\n" +
                            "Details: " + agoRapideAttribute.ToString());
                    };
                } else if (CoreProperty == CoreProperty.None) {
                    ValidatorAndParser = value => {
                        throw new NotImplementedException(
                            "Validator for " + PExplained + " is not implemented because no " + nameof(CoreProperty) + " was given (" + typeof(T) + "." + P + "is assumed irrelevant as entity property enum).\r\n" +
                            "Details: " + agoRapideAttribute.ToString());
                    };
                } else {
                    if (A.Type.Equals(typeof(string))) {
                        ValidatorAndParser = value => !string.IsNullOrEmpty(value) ? new ParseResult(new Property(CoreProperty, value), value) : new ParseResult("Illegal as string (" + (value == null ? "[NULL]" : "[EMPTY]") + ")");
                    } else if (A.Type.Equals(typeof(int))) {
                        throw new TypeIntNotSupportedByAgoRapideException(A.ToString());
                        // ValidatorAndParser = value => int.TryParse(value, out var intValue) ? new ParseResult(new Property(P, intValue), intValue) : new ParseResult("Illegal as int");
                    } else if (A.Type.Equals(typeof(long))) {
                        ValidatorAndParser = value => long.TryParse(value, out var lngValue) ? new ParseResult(new Property(CoreProperty, lngValue), lngValue) : new ParseResult("Illegal as long");
                    } else if (A.Type.Equals(typeof(double))) {
                        ValidatorAndParser = value => {
                            throw new NotImplementedException(
                                "Validator for type " + A.Type.ToStringShort() + " is NotYetImplemented.\r\n" +
                                "Details: " + agoRapideAttribute.ToString());
                        };
                    } else if (A.Type.Equals(typeof(bool))) {
                        ValidatorAndParser = value => bool.TryParse(value, out var blnValue) ? new ParseResult(new Property(CoreProperty, blnValue), blnValue) : new ParseResult("Illegal as boolean, use '" + true.ToString() + "' or '" + false.ToString() + "'");
                    } else if (A.Type.Equals(typeof(DateTime))) {
                        var validFormats = Util.Configuration.ValidDateFormatsByResolution.GetValue2(A.DateTimeFormat);
                        ValidatorAndParser = value => DateTime.TryParseExact(value, validFormats, Util.Configuration.Culture, System.Globalization.DateTimeStyles.None, out var dtmValue) ? new ParseResult(new Property(CoreProperty, dtmValue), dtmValue) : new ParseResult(
                            "Invalid as " + A.Type + ".\r\n" +
                            "Must be in one of the following formats:\r\n" +
                            string.Join(", ", validFormats) + "\r\n");
                    } else if (A.Type.IsEnum) {
                        ValidatorAndParser = value => Util.EnumTryParse(A.Type, value, out var enumValue) ? new ParseResult(new Property(CoreProperty, enumValue), enumValue) : new ParseResult(
                            "Invalid as " + A.Type + ".\r\n" +
                            "Must be one of\r\n" +
                            string.Join(", ", A.ValidValues) + "\r\n");
                    } else {
                        // TODO: Try something like a general TryParse through reflection
                        ValidatorAndParser = value => {
                            throw new NotImplementedException(
                                "Validator for type " + A.Type.ToStringShort() + " is not implemented because that type is unknown.\r\n" +
                                "Details: " + agoRapideAttribute.ToString());
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Converts the object to a string-representation. 
        /// 
        /// Typically used before calling <see cref="APIMethod.GetAPICommand"/>
        /// 
        /// TOOD: Look for duplicate code elsewhere in system. 
        /// 
        /// TODO: Implement for more than string or integer
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string ConvertObjectToString(object obj) {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (A.Type == null) throw new NullReferenceException(nameof(A.Type) + " for " + A.ToString());
            if (!A.Type.IsAssignableFrom(obj.GetType())) {
                if (typeof(ITypeDescriber).IsAssignableFrom(A.Type)) { /// Make exception if we succeed in parsing the value found.
                    switch (obj) {
                        case string objAsString:
                            var parseResult = ValidatorAndParser(objAsString);
                            if (parseResult.ErrorResponse == null) return objAsString; // string is valid
                            break;
                    }
                }
                InvalidTypeException.AssertAssignable(obj.GetType(), A.Type, () => A.ToString());
            }
            if (A.Type.Equals(typeof(DateTime))) return (obj as DateTime? ?? throw new NullReferenceException(nameof(obj) + " for " + A.ToString())).ToString(DateTimeFormat.DateHourMin);
            if (A.Type.Equals(typeof(double))) return (obj as double? ?? throw new NullReferenceException(nameof(obj) + " for " + A.ToString())).ToString2();
            // Type of object is unknown, we just have to trust that ToString is good enough for use in an URL.
            return obj.ToString();
        }

        ///// <summary>
        ///// Translate types given in <see cref="AgoRapideAttribute.Type"/> and <see cref="AgoRapideAttribute.Parents"/> in contexts where 
        ///// TProperty is not known. 
        ///// 
        ///// TODO: Create some generic function here that looks for generic types of CoreProperty and replaces
        ///// TODO: that by constructing the corresponding type with TProperty.
        ///// </summary>
        ///// <param name="type"></param>
        ///// <returns></returns>
        //private static Type TranslateType(Type type) {
        //    if (type.Equals(typeof(CoreProperty))) return typeof(TProperty);
        //    /// Special generic classes:
        //    if (type.Equals(typeof(QueryId<CoreProperty>))) return typeof(QueryId);
        //    if (type.Equals(typeof(IntegerQueryId<CoreProperty>))) return typeof(IntegerQueryId);
        //    if (type.Equals(typeof(PropertyValueQueryId<CoreProperty>))) return typeof(PropertyValueQueryId);
        //    if (type.Equals(typeof(Request<CoreProperty>))) return typeof(Request); // 

        //    /// Ordinary <see cref="BaseEntityT"/> classes:
        //    if (type.Equals(typeof(APIMethod<CoreProperty>))) return typeof(APIMethod);
        //    if (type.Equals(typeof(APIMethodCandidate<CoreProperty>))) return typeof(APIMethodCandidate);
        //    if (type.Equals(typeof(ApplicationPart<CoreProperty>))) return typeof(ApplicationPart);
        //    if (type.Equals(typeof(BaseEntityT<CoreProperty>))) return typeof(BaseEntityT);
        //    if (type.Equals(typeof(BaseEntityTWithLogAndCount<CoreProperty>))) return typeof(BaseEntityTWithLogAndCount);
        //    if (type.Equals(typeof(ClassAndMethod<CoreProperty>))) return typeof(ClassAndMethod);
        //    if (type.Equals(typeof(EnumClass<CoreProperty>))) return typeof(EnumClass);
        //    if (type.Equals(typeof(GeneralQueryResult<CoreProperty>))) return typeof(GeneralQueryResult);
        //    if (type.Equals(typeof(Parameters<CoreProperty>))) return typeof(Parameters);
        //    if (type.Equals(typeof(Property<CoreProperty>))) return typeof(Property);
        //    if (type.Equals(typeof(Result<CoreProperty>))) return typeof(Result);

        //    return type;
        //}

        /// <summary>
        /// Cleanup of values, to be used before value is attempted validated
        /// 
        /// Note that for some types (like long, double, bool, DateTime, string) <see cref="AgoRapideAttributeT(AgoRapideAttribute)"/> 
        /// will set a standard <see cref="Cleaner"/> automatically (may be overriden afterwards or others chained to it)
        /// TODO: IMPLEMENT CHAINING OF CLEANING!
        /// </summary>
        public Func<string, string> Cleaner { get; set; }

        /// <summary>
        /// Note that for some types (like long, double, bool, DateTime) <see cref="AgoRapideAttributeT(AgoRapideAttribute)"/> 
        /// will set a standard <see cref="ValidatorAndParser"/> automatically (may be overriden afterwards or others chained to it)
        /// TODO: IMPLEMENT CHAINING OF VALIDATING!
        /// 
        /// TODO: WE ALSO NEED A TYPE SPECIFIC Validator FOR INSTANCE FOR long-value that 
        /// TODO: must be between 1 and 10. Such a validator will for instance be ignored by
        /// TODO: property.Initialize at the moment (Dec 2016)
        /// </summary>
        public Func<string, ParseResult> ValidatorAndParser { get; set; }

        /// <summary>
        /// TODO: We could add an overload AgoRapideAttribute.TryValidate with Property as parameter instead
        /// TODO: This could be smarter by not attempting to call validator for already set
        /// TODO: long, double, DateTime values
        ///
        /// Note that TryValidateAndParse returns TRUE if no ValidatorAndParser is available
        /// TODO: This is not good enough. FALSE would be a better result (unless TProperty is string)
        /// 
        /// TODO: IMPLEMENT CHAINING OF VALIDATING!
        /// 
        // TODO: Try to MAKE A.TryValidateAndParse GENERIC in order for it to return a more strongly typed result.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result">Will always be set</param>
        /// <returns></returns>
        public bool TryValidateAndParse(string value, out ParseResult result) {
            A.AssertProperty();
            /// TODO: This is not good enough. We should not accept not having a validator and parser...
            // if (ValidatorAndParser == null) { result = new ParseResult(new Property(P, value)); return true; }
            // Therefore we do this:
            if (ValidatorAndParser == null) throw new NullReferenceException(nameof(ValidatorAndParser) + ". Details: " + ToString());
            result = ValidatorAndParser(value);
            if (result.ErrorResponse != null) result = new ParseResult("Validation failed for " + PToString + " = '" + value + "'.\r\nDetails: " + result.ErrorResponse);
            return result.ErrorResponse == null;
        }

        /// <summary>
        /// TODO: Try to MAKE A.TryValidateAndParse GENERIC in order for it to return a more strongly typed result.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result">Will always be set, regardless of TRUE / FALSE as return value</param>
        /// <returns></returns>
        public bool TryCleanAndValidateAndParse(string value, out ParseResult result) {
            A.AssertProperty();
            var originalValue = Cleaner == null ? null : value;
            if (Cleaner != null) value = Cleaner(value);
            // TODO: Try to MAKE A.TryValidateAndParse GENERIC in order for it to return a more strongly typed result.
            var retval = TryValidateAndParse(value, out result);
            if (!retval && originalValue != null && !originalValue.Equals(value)) {
                result = new ParseResult(result.ErrorResponse + ".\r\nValue was unsuccessfully first cleaned up from originalValue '" + originalValue + "'");
            }
            return retval;
        }
    }

    /// <summary>
    /// Contains either <see cref="Result"/> or <see cref="ErrorResponse"/>
    /// </summary>
    public class ParseResult {

        public Property Result { get; private set; }

        /// <summary>
        /// Corresponds to <see cref="Result"/>.<see cref="Property.ADotTypeValue"/>
        /// TODO: Ideally we would like to do without this parameter but that would lead to <see cref="Property.ADotTypeValue"/> 
        /// calling itself 
        /// (see code line
        ///    aDotTypeValue = KeyA.TryValidateAndParse(V<string>(), out var temp) ? temp.ObjResult : null;
        /// )
        /// </summary>
        public object ObjResult { get; private set; }

        /// <summary>
        /// Will be null if Result is set
        /// </summary>
        public string ErrorResponse { get; private set; }

        public ParseResult(Property result, object objResult) {
            Result = result ?? throw new NullReferenceException(nameof(result));
            ObjResult = objResult ?? throw new NullReferenceException(nameof(objResult));
            ErrorResponse = null;
        }
        public ParseResult(string errorResponse) {
            ErrorResponse = errorResponse ?? throw new NullReferenceException(nameof(errorResponse));
            Result = null;
        }
    }
}

