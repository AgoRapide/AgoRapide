using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: Consider renaming into A (and likewise <see cref="AgoRapideAttributeEnrichedT{T}"/> into AT).
    /// 
    /// TODO: Consider implementing a separate class only for entity enum properties like <see cref="CoreProperty"/> and P 
    /// 
    /// Extends on <see cref="AgoRapideAttribute"/> because that class is very limited since it is an <see cref="Attribute"/>-class.  
    /// See also subclass <see cref="AgoRapideAttributeEnrichedT{T}"/>.
    /// 
    /// TODO: As of Jan 2017 there is still some work to be done in this class regarding parsing and validation
    ///
    /// TODO: Make this inherit <see cref="BaseEntityT"/> and store Properties to database. In this manner we
    /// TODO: get HISTORICAL information about documentation (for each and every attribute of a property), giving us much
    /// TODO: better documentation of the application.    
    /// TODO: ACTUALLY, make this inherit <see cref="ApplicationPart"/> (with its own id against database)
    /// TODO: and with its own <see cref="CoreMethod"/> called AgoRapideAttribute.
    /// 
    /// TODO: (AFTER IMPLEMENTING ABOVE) MOVE THIS TO ENTITY-FOLDER SINCE INHERITS <see cref="BaseEntityT"/> 
    /// 
    /// TODO: SPLIT <see cref="AgoRapideAttribute"/> into EnumAttribute and ClassAttribute.
    /// </summary>
    public abstract class AgoRapideAttributeEnriched {
        
        public AgoRapideAttribute A { get; protected set; }

        /// <summary>
        /// The value used in API queries, for storing in database and so on.
        /// Corresponds directly <see cref="AgoRapideAttributeEnrichedT{T}.P"/> (if this is actually an instance of that class)
        /// </summary>
        public string PToString { get; protected set; }

        /// <summary>
        /// Explains how this originates. 
        /// 
        /// Typical examples:
        /// CoreProperty.Username
        /// P.Email &lt;- CoreProperty.Username (when <see cref="AgoRapideAttribute.InheritAndEnrichFromProperty"/> is used)
        /// P.FirstName (CoreProperty 42) (when no corresponding <see cref="CoreProperty"/> exists. 
        /// TODO: If very high value (like almost MaxInt), then explain this as a IsMany-property where P is the index
        /// </summary>
        public string PExplained { get; protected set; }

        public CoreProperty? _coreProperty;
        /// <summary>
        /// Throws exception if <see cref="_coreProperty"/> not set. 
        /// 
        /// TODO: Split <see cref="AgoRapideAttributeEnriched"/> into multiple classes.
        /// </summary>
        public CoreProperty CoreProperty => _coreProperty ?? throw new NullReferenceException(
            nameof(CoreProperty) + ". " +
            "This property is only set for entity property enums through " + nameof(EnumMapper) + "." + nameof(EnumMapper.MapEnum) + ".\r\n" + 
            "For other enums it is irrelevant (illegal) to ask for " + nameof(CoreProperty) + ".\r\n" +
            "Details:\r\n" + A.ToString());

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
            return obj.ToString(); // int and enums for instance should work quite OK now.
        }

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
        
        public void Initialize() {

            PToString = A.Property.ToString();
            PExplained = A.Property.GetType().ToStringVeryShort() + "." + PToString;
            if (_coreProperty != null && !A.Property.GetType().Equals(typeof(CoreProperty))) PExplained += " (" + nameof(CoreProperty) + ": " + _coreProperty.ToString() + ")";

            /// Enrichment 1, explicit given
            /// -----------------------------------------
            if (!string.IsNullOrEmpty(A.InheritAndEnrichFromProperty)) {
                if (A.Property.Equals(A.InheritAndEnrichFromProperty)) throw new InvalidMappingException(nameof(A) + "." + nameof(A.Property) + " (" + A.Property + ").Equals(" + nameof(A) + "." + nameof(A.InheritAndEnrichFromProperty) + ")\r\nDetails: " + ToString());
                var cpa = EnumMapper.GetCPA(A.InheritAndEnrichFromProperty);
                A.EnrichFrom(cpa.A);
                PExplained += " <- " + cpa.PExplained;
            }

            /// Enrichment 2, from enum-"class" 
            /// (see <see cref="CoreProperty.CoreMethod"/> / <see cref="CoreMethod"/> for example)
            /// (note how both enrichment 2 and 4 is based on <see cref="AgoRapideAttribute.Type"/>)
            /// -----------------------------------------
            if (A.Type == null) {
                // Nothing to enrich from 
            } else {
                A.Type.GetAgoRapideAttribute().Use(a => {
                    if (a.IsDefault) return; // Nothing interesting / nothing of value
                    A.EnrichFrom(a); /// Some of the properties for <see cref="AgoRapideAttribute"/> are not relevant in this case, like <see cref="IsMany"/>
                    PExplained += " (also enriched from type " + A.Type.ToStringShort() + ")";
                });
            }

            /// Enrichment 3, from <see cref="IGroupDescriber"/>
            /// -----------------------------------------
            if (A.Group != null) {
                InvalidTypeException.AssertAssignable(A.Group, typeof(IGroupDescriber), () => "Type given as " + typeof(AgoRapideAttribute).ToString() + "." + nameof(AgoRapideAttribute.Group) + " to " + typeof(CoreProperty).ToString() + "." + A.Property + " must implement " + typeof(Core.IGroupDescriber));
                try {
                    ((IGroupDescriber)Activator.CreateInstance(A.Group)).EnrichAttribute(this);
                    PExplained += " (also enriched from " + nameof(IGroupDescriber) + " " + A.Group.ToStringShort() + ")";
                } catch (Exception ex) {
                    throw new AgoRapideAttributeException(
                        "Unable to initialize instance of " + A.Group + " given as " + typeof(AgoRapideAttribute).ToString() + "." + nameof(AgoRapideAttribute.Group) + " to " + typeof(CoreProperty).ToString() + "." + A.Property + ".\r\n" +
                        "Most probably because " + A.Group + " does not have a default constructor without any arguments\r\n" +
                        "Details:\r\n" + A.ToString(), ex);
                }
            }

            /// Enrichment 4, from <see cref="ITypeDescriber"/>
            /// (note how both enrichment 2 and 4 is based on <see cref="AgoRapideAttribute.Type"/>)
            /// -----------------------------------------
            if (A.Type != null && typeof(ITypeDescriber).IsAssignableFrom(A.Type)) {
                var methodName = nameof(IGroupDescriber.EnrichAttribute); /// Note that <see cref="ITypeDescriber"/> itself is "empty".
                try {
                    var method = A.Type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) ?? throw new InvalidTypeException(A.Type, "Does not have a public static method called " + methodName);
                    method.Invoke(null, new object[] { this });
                    PExplained += " (also enriched from " + nameof(ITypeDescriber) + " " + A.Type.ToStringShort() + ")";
                } catch (Exception ex) {
                    throw new AgoRapideAttributeException(
                        "Unable to invoke \r\n" + A.Type.ToStringShort() + "'s\r\n" +
                        "   public static void method " + methodName + "\r\n" +
                        "given as " + typeof(AgoRapideAttribute).ToString() + "." + nameof(AgoRapideAttribute.Type) + " to " + typeof(CoreProperty).ToString() + "." + A.Property + "\r\n" +
                        "Resolution: Check that it exists and that it takes exactly one parameter of type " + typeof(AgoRapideAttributeEnriched).ToStringShort() + ".\r\n" +
                        "In other words it should look like\r\n\r\n" +
                        "   public static void method " + methodName + "(" + typeof(AgoRapideAttributeEnriched).ToStringShort() + " agoRapideAttribute)\r\n\r\n" +
                        "Details:\r\n" + A.ToString(), ex);
                }
            }

            /// Enrichment 5, autonomous 
            /// (deduced from already known information)
            /// -----------------------------------------
            if (A.ValidValues == null && A.Type != null) {
                if (A.Type.IsEnum) {
                    A.ValidValues = Util.EnumGetValues(A.Type).ToArray();
                    if (A.SampleValues != null) {
                        throw new AgoRapideAttributeException(
                            "It is illegal (unnecessary) to combine " + nameof(A.SampleValues) + " with Type.IsEnum (" + A.Type.ToStringShort() + ") " +
                            "because " + nameof(A.ValidValues) + " can be used instead.\r\n" +
                            "Details:\r\n" + A.ToString());
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
                    A.Type.Equals(typeof(string))  // TODO: Switch is unnecessary anyway, code below is identical to code within the final else anyway...
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

            // TODO: Build validatorAndParser in chains
            // TODO: For instance general cleaner that removes white space, 
            // TODO: then general long-parser and at last validator for value range based on min / max values.

            if (!string.IsNullOrEmpty(A.RegExpValidator)) throw new NotImplementedException(
                typeof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.RegExpValidator) + "\r\n" +
                "Details: " + A.ToString());

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
                            "Details: " + A.ToString());
                    };
                //} else if (CoreProperty == CoreProperty.None) {
                //    ValidatorAndParser = value => {
                //        throw new NotImplementedException(
                //            "Validator for " + PExplained + " is not implemented because no " + nameof(CoreProperty) + " was given (" + PExplained + " is assumed irrelevant as entity property enum).\r\n" +
                //            "Details: " + A.ToString());
                //    };
                } else {
                    if (A.Type.Equals(typeof(string))) {
                        ValidatorAndParser = value => !string.IsNullOrEmpty(value) ? new ParseResult(new Property(this, value), value) : new ParseResult("Illegal as string (" + (value == null ? "[NULL]" : "[EMPTY]") + ")");
                    } else if (A.Type.Equals(typeof(int))) {
                        throw new TypeIntNotSupportedByAgoRapideException(A.ToString());
                        // ValidatorAndParser = value => int.TryParse(value, out var intValue) ? new ParseResult(new Property(P, intValue), intValue) : new ParseResult("Illegal as int");
                    } else if (A.Type.Equals(typeof(long))) {
                        ValidatorAndParser = value => long.TryParse(value, out var lngValue) ? new ParseResult(new Property(this, lngValue), lngValue) : new ParseResult("Illegal as long");
                    } else if (A.Type.Equals(typeof(double))) {
                        ValidatorAndParser = value => {
                            throw new NotImplementedException(
                                "Validator for type " + A.Type.ToStringShort() + " is NotYetImplemented.\r\n" +
                                "Details: " + A.ToString());
                        };
                    } else if (A.Type.Equals(typeof(bool))) {
                        ValidatorAndParser = value => bool.TryParse(value, out var blnValue) ? new ParseResult(new Property(this, blnValue), blnValue) : new ParseResult("Illegal as boolean, use '" + true.ToString() + "' or '" + false.ToString() + "'");
                    } else if (A.Type.Equals(typeof(DateTime))) {
                        var validFormats = Util.Configuration.ValidDateFormatsByResolution.GetValue2(A.DateTimeFormat);
                        ValidatorAndParser = value => DateTime.TryParseExact(value, validFormats, Util.Configuration.Culture, System.Globalization.DateTimeStyles.None, out var dtmValue) ? new ParseResult(new Property(this, dtmValue), dtmValue) : new ParseResult(
                            "Invalid as " + A.Type + ".\r\n" +
                            "Must be in one of the following formats:\r\n" +
                            string.Join(", ", validFormats) + "\r\n");
                    } else if (A.Type.IsEnum) {
                        ValidatorAndParser = value => Util.EnumTryParse(A.Type, value, out var enumValue) ? new ParseResult(new Property(this, enumValue), enumValue) : new ParseResult(
                            "Invalid as " + A.Type + ".\r\n" +
                            "Must be one of the following values:\r\n" +
                            string.Join(", ", A.ValidValues) + "\r\n");
                    } else {
                        // TODO: Try something like a general TryParse through reflection
                        ValidatorAndParser = value => {
                            throw new NotImplementedException(
                                "Validator for type " + A.Type.ToStringShort() + " is not implemented because that type is unknown.\r\n" +
                                "Details: " + A.ToString());
                        };
                    }
                }
            }
        }
    }
}