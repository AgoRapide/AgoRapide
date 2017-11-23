// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;

namespace AgoRapide.Core {

    /// <summary>
    /// Extends on <see cref="PropertyKeyAttribute"/> because that class is very limited since it is an <see cref="Attribute"/>-class.  
    /// 
    /// TODO: Candidate for removal. Put functionality into <see cref="PropertyKey"/> instead.
    /// 
    /// See subclasses 
    /// <see cref="PropertyKeyAttributeEnrichedT{T}"/>: Attribute originating from C# code.
    /// <see cref="PropertyKeyAttributeEnrichedDyn"/>: <see cref="AggregationKey"/> or attribute originating dynamically (from database / API client, not C# code)
    /// 
    /// TODO: As of Jan 2017 there is still some work to be done in this class regarding parsing and validation
    /// </summary>
    public abstract class PropertyKeyAttributeEnriched {

        public PropertyKeyAttribute A { get; protected set; }

        /// <summary>
        /// The value used in API queries, for storing in database and so on.
        /// Corresponds directly <see cref="PropertyKeyAttributeEnrichedT{T}.P"/> (if this is actually an instance of that class)
        /// </summary>
        public string PToString { get; protected set; }

        public CoreP? _coreP;
        /// <summary>
        /// Throws exception if <see cref="_coreP"/> not set. 
        /// </summary>
        public CoreP CoreP => _coreP ?? throw new NullReferenceException(nameof(_coreP) + ".\r\nDetails: " + ToString()); /// Set by methods like <see cref="PropertyKeyAttributeEnrichedT{T}.PropertyKeyAttributeEnrichedT"/>

        private ConcurrentDictionary<Type, bool> _hasParentOfTypeCache = new ConcurrentDictionary<Type, bool>();
        /// <summary>
        /// Explains if this property belongs to the given entity type (based on <see cref="PropertyKeyAttribute.Parents"/>). 
        /// Used by <see cref="Extensions.GetObligatoryChildProperties(Type)"/>. 
        /// 
        /// TODO: Consider moving to <see cref="PropertyKeyAttribute"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool HasParentOfType(Type type) => _hasParentOfTypeCache.GetOrAdd(type, t => A.Parents != null && A.Parents.Any(p => p.IsAssignableFrom(t)));

        /// <summary>
        /// Typically used by an <see cref="IGroupDescriber"/>.
        /// Assumed to be used only at application initialization.
        /// </summary>
        /// <param name="type"></param>
        public void AddParent(Type type) {
            Util.AssertCurrentlyStartingUp();
            InvalidTypeException.AssertAssignable(type, typeof(BaseEntity));
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

            if (A.Type.Equals(typeof(string))) { // Added 2 Jun 2017 (equivalent to code at end of method)
                switch (obj) {
                    case DateTime dtm: return dtm.ToString(DateTimeFormat.DateHourMin);
                    case double dbl: return dbl.ToString2();
                    default: return obj.ToString(); // int and enums for instance should work quite OK now.
                }
            }

            if (!A.Type.IsAssignableFrom(obj.GetType())) {

                if (typeof(ITypeDescriber).IsAssignableFrom(A.Type)) { /// Make exception if we succeed in parsing the value found.
                    switch (obj) {
                        case string objAsString:
                            var parseResult = ValidatorAndParser(objAsString);
                            if (parseResult.ErrorResponse == null) return objAsString; // string is valid
                            break;
                    }
                }

                throw new InvalidObjectTypeException(obj, A.Type,
                    ((obj is CoreP && typeof(PropertyKey).IsAssignableFrom(A.Type)) ? "A common mistake is specifying " + typeof(CoreP) + " (like " + nameof(CoreP) + ".SomeValue) instead of " + typeof(PropertyKey) + " (like " + nameof(CoreP) + ".SomeValue." + nameof(CorePExtension.A) + "()).\r\n" : "") +
                    A.ToString());
            }
            if (A.Type.Equals(typeof(DateTime))) return (obj as DateTime? ?? throw new NullReferenceException(nameof(obj) + " for " + A.ToString())).ToString(DateTimeFormat.DateHourMin);
            if (A.Type.Equals(typeof(double))) return (obj as double? ?? throw new NullReferenceException(nameof(obj) + " for " + A.ToString())).ToString2();
            if (A.Type.Equals(typeof(Type))) return (obj as Type ?? throw new NullReferenceException(nameof(obj) + " for " + A.ToString())).ToStringDB(); // Added 19 Jun 2017
            // Type of object is unknown, we just have to trust that ToString is good enough for use in an URL.
            return obj.ToString(); // int and enums for instance should work quite OK now.
        }

        /// <summary>
        /// Cleanup of values, to be used before value is attempted validated. 
        /// 
        /// Note that for some types (like long, double, bool, DateTime, string) <see cref="Initialize"/> 
        /// will set a standard <see cref="Cleaner"/> automatically (may be overriden afterwards or others chained to it)
        /// TODO: IMPLEMENT CHAINING OF CLEANING!
        /// </summary>
        public Func<string, string> Cleaner { get; set; }

        /// <summary>
        /// Validates and parses a value.
        /// 
        /// Note that for some types (like long, double, bool, DateTime) <see cref="Initialize"/> 
        /// will set a standard <see cref="ValidatorAndParser"/> automatically (may be overriden afterwards or others chained to it)
        /// TODO: IMPLEMENT CHAINING OF VALIDATING!
        /// 
        /// TODO: WE ALSO NEED A TYPE SPECIFIC Validator FOR INSTANCE FOR long-value that 
        /// TODO: must be between 1 and 10. Such a validator will for instance be ignored by
        /// TODO: property.Initialize at the moment (Dec 2016)
        /// </summary>
        public Func<string, ParseResult> ValidatorAndParser { get; set; }

        public bool TryValidateAndParse(string value, out ParseResult result) {
            A.AssertProperty();
            if (ValidatorAndParser == null) throw new NullReferenceException(nameof(ValidatorAndParser) + ". Details: " + ToString());
            result = ValidatorAndParser(value);
            if (result.ErrorResponse != null) result = ParseResult.Create("Validation failed for " + PToString + " = '" + value + "'.\r\nDetails: " + result.ErrorResponse);
            return result.ErrorResponse == null;
        }

        /// <summary>
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
                result = ParseResult.Create(result.ErrorResponse + ".\r\nValue was unsuccessfully first cleaned up from originalValue '" + originalValue + "'");
            }
            return retval;
        }

        public void Initialize() {
            PToString = A.EnumValue.ToString();
            A.SetEnumValueExplained(A.EnumValue.GetType().ToStringVeryShort() + "." + PToString);
            // TODO: Clean up code for documentation here.
            if (_coreP != null && A.InheritFrom == null && !A.EnumValue.GetType().Equals(typeof(CoreP))) {
                A.SetEnumValueExplained(A.EnumValueExplained + " (" + nameof(CoreP) + ": " + _coreP.ToString() + ")");
            }

            if (A.ExternalPrimaryKeyOf != null) {
                AddParent(A.ExternalPrimaryKeyOf);
                A.IsExternal = true;
            }

            if (A.ExternalForeignKeyOf != null) {
                // AddParent(A.ExternalForeignKeyOf) is of course not relevant now
                A.IsExternal = true;
            }

            if (A.Parents != null) A.Parents.ForEach(t => InvalidTypeException.AssertAssignable(t, typeof(BaseEntity), () => "Invalid as parent, only " + typeof(BaseEntity) + " derived types may be given as parent. Details: " + ToString()));

            /// Enrichment 1, explicit given
            /// -----------------------------------------
            if (A.InheritFrom != null) {
                //if ("Email".Equals(PToString)) {
                //    var a = 1;
                //}
                NotOfTypeEnumException.AssertEnum(A.InheritFrom.GetType(), () => nameof(A.InheritFrom) + "\r\n" + ToString());
                if (A.EnumValue.Equals(A.InheritFrom)) throw new InvalidMappingException(nameof(A) + "." + nameof(A.EnumValue) + " (" + A.EnumValue + ").Equals(" + nameof(A) + "." + nameof(A.InheritFrom) + ")\r\nDetails: " + ToString());
                var key = PropertyKeyMapper.GetA(A.InheritFrom.ToString());
                _coreP = key.Key.CoreP;
                A.EnrichFrom(key.Key.A);
                A.SetEnumValueExplained(A.EnumValueExplained + " <- " + key.Key.A.EnumValueExplained);
            }

            /// Enrichment 2, from enum-"class" 
            /// (see <see cref="CoreP.CoreAPIMethod"/> / <see cref="CoreAPIMethod"/> for example)
            /// (note how both enrichment 2 and 4 is based on <see cref="PropertyKeyAttribute.Type"/>)
            /// -----------------------------------------
            if (!A.TypeIsSet) {
                // Nothing to enrich from 
            } else {
                if (A.Type.IsEnum) {
                    A.Type.GetEnumAttribute().Use(a => {
                        if (a.IsDefault) return; // Nothing interesting / nothing of value
                        A.EnrichFrom(a);
                        A.SetEnumValueExplained(A.EnumValueExplained + " (also enriched from enum " + A.Type.ToStringShort() + ")");
                    });
                } else {
                    A.Type.GetClassAttribute().Use(a => {
                        if (a.IsDefault) return; // Nothing interesting / nothing of value
                        A.EnrichFrom(a);
                        A.SetEnumValueExplained(A.EnumValueExplained + " (also enriched from class " + A.Type.ToStringShort() + ")");
                    });
                }
            }

            /// Enrichment 3, from <see cref="IGroupDescriber"/>
            /// -----------------------------------------
            if (A.Group != null) {
                InvalidTypeException.AssertAssignable(A.Group, typeof(IGroupDescriber), () => "Type given as " + typeof(PropertyKeyAttribute).ToString() + "." + nameof(PropertyKeyAttribute.Group) + " to " + typeof(CoreP).ToString() + "." + A.EnumValue + " must implement " + typeof(Core.IGroupDescriber));
                try {
                    ((IGroupDescriber)Activator.CreateInstance(A.Group)).EnrichKey(this);
                    A.SetEnumValueExplained(A.EnumValueExplained + " (also enriched from " + nameof(IGroupDescriber) + " " + A.Group.ToStringShort() + ")");
                } catch (Exception ex) {
                    throw new BaseAttribute.AttributeException(
                        "Unable to initialize instance of " + A.Group + " given as " + typeof(PropertyKeyAttribute).ToString() + "." + nameof(PropertyKeyAttribute.Group) + " to " + typeof(CoreP).ToString() + "." + A.EnumValue + ".\r\n" +
                        "Most probably because " + A.Group + " does not have a default constructor without any arguments\r\n" +
                        "Details:\r\n" + A.ToString(), ex);
                }
            }

            /// Enrichment 4, from <see cref="ITypeDescriber"/>
            /// (note how both enrichment 2 and 4 is based on <see cref="PropertyKeyAttribute.Type"/>)
            /// -----------------------------------------
            if (A.TypeIsSet && typeof(ITypeDescriber).IsAssignableFrom(A.Type)) {
                var methodName = nameof(IGroupDescriber.EnrichKey); /// Note that <see cref="ITypeDescriber"/> itself is "empty".
                try {
                    var method = A.Type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) ?? throw new InvalidTypeException(A.Type, "Does not have a public static method called " + methodName);
                    method.Invoke(null, new object[] { this });
                    A.SetEnumValueExplained(A.EnumValueExplained + " (also enriched from " + nameof(ITypeDescriber) + " " + A.Type.ToStringShort() + ")");
                } catch (Exception ex) {
                    throw new BaseAttribute.AttributeException(
                        "Unable to invoke \r\n" + A.Type.ToStringShort() + "'s\r\n" +
                        "   public static void method " + methodName + "\r\n" +
                        "given as " + typeof(PropertyKeyAttribute).ToString() + "." + nameof(PropertyKeyAttribute.Type) + " to " + typeof(CoreP).ToString() + "." + A.EnumValue + "\r\n" +
                        "Resolution: Check that it exists and that it takes exactly one parameter of type " + typeof(PropertyKeyAttributeEnriched).ToStringShort() + ".\r\n" +
                        "In other words it should look like\r\n\r\n" +
                        "   public static void " + methodName + "(" + typeof(PropertyKeyAttributeEnriched).ToStringShort() + " key)\r\n\r\n" +
                        "Details:\r\n" + A.ToString(), ex);
                }
            }

            /// Enrichment 5, autonomous 
            /// (deduced from already known information)
            /// -----------------------------------------
            if (A.ValidValues == null && A.TypeIsSet) {
                var asserter = new Action(() => {
                    if (A.SampleValues != null) {
                        throw new BaseAttribute.AttributeException( // TODO: Check validity of this
                            "It is illegal (unnecessary) to combine " + nameof(A.SampleValues) + " with type (" + A.Type.ToStringShort() + ") " +
                            "because " + nameof(A.ValidValues) + " can (and will be) used instead.\r\n" +
                            "Details:\r\n" + A.ToString());
                    }
                });
                if (A.Type.IsEnum) {
                    asserter();
                    A.ValidValues = Util.EnumGetNames(A.Type).ToArray();
                    A.SampleValues = A.ValidValues;
                } else if (typeof(bool).Equals(A.Type)) {
                    asserter();
                    A.ValidValues = new string[] { true.ToString(), false.ToString() };
                    A.SampleValues = A.ValidValues;
                } else {
                    // Difficult to think of something here
                }
            }

            if (A.ForeignKeyOf != null) {
                A.Description += (string.IsNullOrEmpty(A.Description) ? "" : "\r\n") + "-" + nameof(A.ForeignKeyOf) + "- -" + A.ForeignKeyOf.ToStringShort() + "-.";
            }

            if (A.ExternalForeignKeyOf != null) {
                A.Description += (string.IsNullOrEmpty(A.Description) ? "" : "\r\n") + "-" + nameof(A.ExternalForeignKeyOf) + "- -" + A.ExternalForeignKeyOf.ToStringShort() + "-.";
            }

            if (!A.TypeIsSet) {
                if (A.ForeignKeyOf != null) {
                    A.Type = typeof(long);
                } else if (A.ExternalForeignKeyOf != null) {
                    A.Type = typeof(long);
                } else {
                    A.Type = typeof(string);
                }
            } else {
                if (A.ForeignKeyOf != null) {
                    InvalidTypeException.AssertEquals(A.Type, typeof(long), () => "Only typeof(long) allowed when " + nameof(A.ForeignKeyOf) + " is set.\r\nDetails: " + A.ToString());
                }
                if (A.ExternalForeignKeyOf != null) {
                    // OK, any type of external foreign key is accepted. 
                }
            }

            if (A.AggregationTypes == null) A.AggregationTypes = new AggregationType[0];
            if (A.ExpansionTypes == null) A.ExpansionTypes = new ExpansionType[0];
            if (A.JoinTo == null) A.JoinTo = new Type[0];

            if (!A.HasLimitedRangeIsSet) {
                if (
                    typeof(bool).Equals(A.Type) ||
                    typeof(Type).Equals(A.Type) ||
                    A.Type.IsEnum
                    ) {
                    A.HasLimitedRange = true;
                }
            }

            if (A.Operators == null) {
                if (
                    typeof(string).Equals(A.Type) || // Added 22 Sep 2017
                    typeof(bool).Equals(A.Type) ||
                    typeof(Type).Equals(A.Type) ||
                    A.HasLimitedRange
                    ) {
                    A.Operators = new Operator[] { Operator.EQ }; /// TODO: Decide about <see cref="Operator.NEQ"/>. As of Sep 2017 we use <see cref="SetOperator.Remove"/> as substitute
                } else if (
                    typeof(double).Equals(A.Type) ||
                    typeof(long).Equals(A.Type) ||
                    typeof(DateTime).Equals(A.Type) ||
                    A.Type.IsEnum
                    ) {
                    A.Operators = new Operator[] { Operator.LT, Operator.LEQ, Operator.EQ, Operator.GEQ, Operator.GT }; /// TODO: Decide about <see cref="Operator.NEQ"/>. As of Sep 2017 we use Remove-Contexts as substitute
                }
            }

            if (Cleaner == null) {
                if (
                    A.Type.Equals(typeof(int)) ||
                    A.Type.Equals(typeof(long)) ||
                    A.Type.Equals(typeof(string))  // TODO: Code below is identical to code within the final else anyway...
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
                typeof(PropertyKeyAttribute) + "." + nameof(PropertyKeyAttribute.RegExpValidator) + "\r\n" +
                "Details: " + A.ToString());

            /// Note how ValidatorAndParser-objects are duplicated for each and every <see cref="CoreP"/>, P and so on. 
            // This is assumed to be of little significance however as the number of such enum values is quite limited. 
            if (ValidatorAndParser != null) {
                /// OK, was most probably set through Enrichment 4, from <see cref="ITypeDescriber"/>
            } else {
                if (A.MustBeValidCSharpIdentifier) {
                    if (!typeof(string).Equals(A.Type)) throw new BaseAttribute.AttributeException(nameof(A.MustBeValidCSharpIdentifier) + " can only be combined with " + nameof(A.Type) + " string, not " + A.Type + ". Details: " + A.ToString());
                    ValidatorAndParser = value => InvalidIdentifierException.TryAssertValidIdentifier(value, out var errorResponse) ? ParseResult.Create(this, value) : ParseResult.Create(
                        "Invalid as C# identifier.\r\nDetails: " + errorResponse);
                } else if (typeof(string).Equals(A.Type)) {
                    ValidatorAndParser = value => !string.IsNullOrEmpty(value) ? ParseResult.Create(this, value) : ParseResult.Create(
                        "Invalid as " + A.Type + " (" + (value == null ? "[NULL]" : "[EMPTY]") + ").");
                } else if (typeof(int).Equals(A.Type)) {
                    throw new TypeIntNotSupportedByAgoRapideException(A.ToString());
                    // ValidatorAndParser = value => int.TryParse(value, out var intValue) ? new ParseResult(new Property(P, intValue), intValue) : new ParseResult("Illegal as int");
                } else if (typeof(long).Equals(A.Type)) {
                    ValidatorAndParser = value => long.TryParse(value, out var temp) ? ParseResult.Create(this, temp) : ParseResult.Create(
                        "Invalid as " + A.Type + ".");
                } else if (typeof(double).Equals(A.Type)) {
                    ValidatorAndParser = value => {
                        throw new NotImplementedException(
                            "Validator for type " + A.Type.ToStringShort() + " is NotYetImplemented.\r\n" +
                            "Details: " + A.ToString());
                    };
                } else if (typeof(bool).Equals(A.Type)) {
                    ValidatorAndParser = value => bool.TryParse(value, out var temp) ? ParseResult.Create(this, temp) : ParseResult.Create(
                        "Invalid as " + A.Type + ", use '" + true.ToString() + "' or '" + false.ToString() + "'");
                } else if (typeof(DateTime).Equals(A.Type)) {
                    var validFormats = Util.Configuration.C.ValidDateFormatsByResolution.GetValue2(A.DateTimeFormat);
                    ValidatorAndParser = value => DateTime.TryParseExact(value, validFormats, Util.Configuration.C.Culture, System.Globalization.DateTimeStyles.None, out var temp) ? ParseResult.Create(this, temp) : ParseResult.Create(
                        "Invalid as " + A.Type + ".\r\n" +
                        "Must be in one of the following formats:\r\n" +
                        string.Join(", ", validFormats) + "\r\n");
                } else if (typeof(TimeSpan).Equals(A.Type)) {
                    ValidatorAndParser = value => TimeSpan.TryParseExact(value, new string[] { @"hh\:mm\:ss", @"d\.hh\:mm\:ss" }, System.Globalization.CultureInfo.InvariantCulture, out var temp) ? ParseResult.Create(this, temp) : ParseResult.Create(
                        Util.BreakpointEnabler + "Invalid as " + A.Type + ".\r\n" +
                        "Must be in one of the following formats:\r\n" +
                        "'hh:mm:ss' or 'd.hh:mm:ss'\r\n"); /// Note corresponding code in <see cref="PropertyT{T}.PropertyT"/> and <see cref="PropertyKeyAttributeEnriched.Initialize"/>
                    // string.Join(", ", validFormats) + "\r\n");
                } else if (typeof(Type).Equals(A.Type)) {
                    ValidatorAndParser = value => Util.TryGetTypeFromString(value, out var temp) ? ParseResult.Create(this, temp) : ParseResult.Create(
                        "Invalid as " + A.Type + " (must be in a format understood by " + nameof(Util) + "." + nameof(Util.TryGetTypeFromString) + ").");
                } else if (typeof(Uri).Equals(A.Type)) {
                    ValidatorAndParser = value => (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var temp)) ? ParseResult.Create(this, temp) : ParseResult.Create(
                        "Invalid as " + A.Type + ".");
                    // TODO: Add any additional desired supported types to this list.
                } else if (A.Type.IsEnum) {
                    ValidatorAndParser = value => {
                        /// <see cref="AgoRapide.CoreP"/> is special because only <see cref="PropertyKeyMapper"/> knows all the mapped values (values mapped towards <see cref="CoreP"/>)
                        if (A.Type.Equals(typeof(CoreP)) && PropertyKeyMapper.TryGetA(value, out var key)) return ParseResult.Create(this, key.Key.CoreP);

                        /// All others enums are parsed in an ordinary manner. 
                        /// NOTE: <see cref="ParseResult.Result"/> now will be <see cref="PropertyT{T}"/> of generic type _OBJECT_.
                        if (Util.EnumTryParse(A.Type, value, out var temp)) return ParseResult.Create(this, temp);
                        return ParseResult.Create(
                            "Invalid as " + A.Type + ".\r\n" +
                            "Must be one of the following values:\r\n" +
                            string.Join(", ", A.ValidValues) + "\r\n"
                        );
                    };
                } else {
                    // TODO: Try something like a general TryParse through reflection
                    ValidatorAndParser = value => {
                        throw new NotImplementedException(
                            "Validator for type " + A.Type.ToStringShort() + " is not implemented because that type is unknown.\r\n" +
                            (!typeof(ITypeDescriber).IsAssignableFrom(A.Type) ? "" : ("Possible cause: " + nameof(IGroupDescriber.EnrichKey) + " may be wrongly implemented for " + A.Type + "\r\n")) + /// Note that <see cref="ITypeDescriber"/> itself is "empty".
                            "Details: " + A.ToString());
                    };
                }
            }

            // Some assertions at end
            if (A.ForeignKeyOf != null) {
                if (A.IsExternal) {
                    throw new BaseAttribute.AttributeException( // TODO: We expect to have to improve on this exception message
                        "It is invalid to combine " + nameof(A.ForeignKeyOf) + " (" + A.ForeignKeyOf + ") with " + nameof(A.IsExternal) + " (" + A.IsExternal + ").\r\n" +
                        nameof(Database.BaseSynchronizer) + "." + nameof(Database.BaseSynchronizer.SynchronizeMapForeignKeys) + " " +
                        "will create these properties automatically as 'CorrespondingInternalKey'-properties.\r\n" +
                        "Details: " + A.ToString());
                }
                if (A.Parents == null || A.Parents.Length == 0) {
                    throw new BaseAttribute.AttributeException(nameof(A.ForeignKeyOf) + " specified without " + nameof(A.Parents) + "\r\nDetails: " + A.ToString());

                    // Misguided, same foreign key can indeed be shared among multiple entities. TODO: REMOVE COMMENTED OUT CODE:
                    //} else if (A.Parents.Length != 1) {
                    //    throw new BaseAttribute.AttributeException(nameof(A.ForeignKeyOf) + " specified with " + A.Parents.Length + " " + nameof(A.Parents) + " (only exact 1 is allowed)\r\nDetails: " + A.ToString());

                }
            }
        }

        /// <summary>
        /// Gets, in a determistic manner, the n'th sample value.
        /// 
        /// Used for mocking purposes. 
        /// </summary>
        /// <param name="n">
        /// 1-based count for which sample values is requested.
        /// </param> 
        /// <param name="maxN">
        /// Maximum number of <paramref name="n"/> that will be used in subsequent calls. 
        /// Used for generation of foreign keys for <see cref="PropertyKeyAttribute.IsExternal"/>. 
        /// </param>
        /// <typeparam name="TParent">
        /// The parent object for which this property is intended. 
        /// Helps with deciding value when <see cref="PropertyKeyAttribute.ExternalPrimaryKeyOf"/>
        /// </typeparam>
        /// <returns></returns>
        public Property GetSampleProperty(Type parentType, int n, Dictionary<Type, int> maxN) {
            var value = new Func<string>(() => {
                var intKeyToStringKeyConverter = new Func<int, string>(v => {
                    if (typeof(long).Equals(A.Type)) {
                        return v.ToString();
                    } else {
                        throw new NotImplementedException(
                            "Foreign key generator for type " + A.Type + " is not implemented.\r\n" +
                            "(Some sources will use keys that are strings for instance, not integers.) " +
                            "Solution: We will have to implement a string generator based on " + nameof(v) + ". This should not be too difficult. " +
                            "We could create a MD5 hash based on the long-value for instance, and use that. " +
                            "(of course we could quite possible just return " + nameof(v) + ".ToString() itself). "
                        );
                    }
                });

                if (A.ExternalPrimaryKeyOf != null) { /// This is the primary key from the <see cref="PropertyKeyAttribute.IsExternal"/>-system
                    InvalidTypeException.AssertEquals(A.ExternalPrimaryKeyOf, parentType, () => nameof(A.ExternalPrimaryKeyOf) + " does not correspond to " + parentType);
                    return intKeyToStringKeyConverter(n); /// Assign id's starting from 1
                }
                if (A.ExternalForeignKeyOf != null) { /// This is the foreign key from the <see cref="PropertyKeyAttribute.IsExternal"/>-system
                    if (!maxN.TryGetValue(A.ExternalForeignKeyOf, out var max)) throw new InvalidTypeException(A.ExternalForeignKeyOf, "Not key in " + nameof(maxN) + " (" + maxN.KeysAsString() + "). Unable to assign a value.");
                    return intKeyToStringKeyConverter(new Random(n).Next(1, max + 1)); /// Assign a deterministic pseudo-random value within maxN for this type. 
                }
                if (A.SampleValues == null || A.SampleValues.Length == 0) throw new SampleGenerationException(this, "No " + nameof(PropertyKeyAttribute.SampleValues) + " available");
                return A.SampleValues[new Random(n).Next(0, A.SampleValues.Length)];
            })();
            if (!TryValidateAndParse(value, out var result)) throw new SampleGenerationException(this, result.ErrorResponse);
            var key = CoreP.A();
            if (!key.Key.A.IsMany) {
                result.Result.SetKey(key.PropertyKeyWithIndex); /// Safe to ask for <see cref="PropertyKey.PropertyKeyWithIndex"/>
            } else { /// Added support for <see cref="PropertyKeyAttribute.IsMany"/> 21 Jun 2017
                result.Result.SetKey(new PropertyKeyWithIndex(key.Key, 1)); // Note how we only create a single value.
            }
            return result.Result;
        }

        public class SampleGenerationException : ApplicationException {
            public SampleGenerationException(PropertyKeyAttributeEnriched key, string message) : base("Unable to generate a sample value for " + key.ToString() + ".\r\nDetails:\r\n" + message) { }
        }

        public override string ToString() => A == null ? ("[" + GetType() + " NOT INITIALIZED CORRECTLY (" + nameof(A) + " == null)]") : A.ToString();
    }
}