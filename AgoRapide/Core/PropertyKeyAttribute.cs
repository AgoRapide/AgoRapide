// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Linq;
using AgoRapide;
using System.Collections.Generic;
using System.ComponentModel;
using AgoRapide.Database;

namespace AgoRapide.Core {

    /// <summary>
    /// See <see cref="EnumType.PropertyKey"/> for more documentation. 
    /// 
    /// TODO: Use more concept like <see cref="IsManyIsSet"/> in order for <see cref="EnrichFrom"/> to know
    /// TODO: which values to enrich.
    /// <see cref="PropertyKeyAttributeEnriched"/>
    /// 
    /// TODO: Implement a lot of properties here as <see cref="CoreP"/> and move documentation there so can be made available via 
    /// TODO: <see cref="BaseAttribute.GetProperties"/>-mechanism in order to push this information out to the API interface.
    /// </summary>
    [Enum(Description =
        "Specialized version of -" + nameof(EnumValueAttribute) + "- which describes an enum value of type -" + nameof(EnumType.PropertyKey) + "- (see that one for documentation). " +
        "Member of -" + nameof(PropertyKey) + "-. (through -" + nameof(PropertyKeyAttributeEnriched) + "-). " +
        "Super class -" + nameof(EnumValueAttribute) + "- describes \"ordinary\" enum values (that is, those NOT of type -" + nameof(EnumType.PropertyKey) + "-)")]
    public class PropertyKeyAttribute : EnumValueAttribute {

        /// <summary>
        /// Default empty constructor for all instances when originates from C# code
        /// </summary>
        public PropertyKeyAttribute() { }

        /// <summary>
        /// Constructor for <see cref="AggregationKey"/> or when originates from database (See <see cref="PropertyKeyMapper.TryAddA"/>)
        /// </summary>
        /// <param name="property"></param>
        /// <param name="description">May be null</param>
        /// <param name="longDescription">May be null</param>
        /// <param name="isMany"></param>
        public PropertyKeyAttribute(
            string property,
            string description,
            string longDescription,
            bool isMany) {

            _enumValue = property ?? throw new ArgumentNullException(nameof(property));
            Description = description;
            LongDescription = longDescription;
            IsMany = isMany;
        }

        private Type _type;
        [ClassMember(
            Description =
                "The type of this Property. Defaults to -" + nameof(String) + "-",
            LongDescription =
                "Typical examples are\r\n" +
                "1) typeof(string), typeof(long), typeof(DateTime),\r\n" +
                "2) typeof(CoreMethod) / typeof([AnyEnum]),\r\n" +
                "3) Can also be a type assignable to -" + nameof(ITypeDescriber) + ",\r\n" +
                "4) or any type understood by " + nameof(PropertyKeyAttributeEnriched) + "\r\n" +
                "\r\n" +
                "Will be set to -" + nameof(String) + "- by " + nameof(PropertyKeyAttributeEnriched.Initialize) + " if not given."
            )]
        public Type Type { get => _type ?? throw new NullReferenceException(nameof(Type) + ". Supposed to always be set from " + nameof(PropertyKeyAttributeEnriched) + ".\r\nDetails: " + ToString()); set => _type = value; }
        /// <summary>
        /// Has little purpose since <see cref="Type"/> will be set anyway by <see cref="PropertyKeyAttributeEnriched.Initialize"/> if not given."
        /// </summary>
        public bool TypeIsSet => _type != null;

        private Type _genericListType;
        [ClassMember(Description = "Returns the corresponding generic List<> type. Only allowed to call when -" + nameof(IsMany) + "-")]
        public Type GenericListType => IsMany ? (_genericListType ?? (_genericListType = typeof(List<>).MakeGenericType(Type))) : throw new IsManyException(Util.BreakpointEnabler + "!" + nameof(IsMany) + ".\r\nDetails: " + ToString());

        /// <summary>
        /// TODO: Is this relevant? Is not <see cref="EnumValue.EnumValue"/> always set for this class?
        /// </summary>
        public void AssertProperty() {
            if (_enumValue == null) throw new NullReferenceException(nameof(_enumValue) + " for " + ToString());
        }

        [ClassMember(Description = "The underlying (more closer to the core AgoRapide library) property that -" + nameof(PropertyKeyAttributeEnriched) + "- will inherit values from.",
            LongDescription =
                "At the same time attributes for that property will be overridden by this -" + nameof(PropertyKeyAttribute) + "- " +
                "(conceptual similar to virtual overridden C# properties). " +
                "The value will often correspond to a -" + nameof(CoreP) + "- value")]
        public object InheritFrom { get; set; }

        public PriorityOrder PriorityOrder { get; set; }

        /// <summary>
        /// TODO: Implement automatic creation of uniqueness index in database in Startup.cs
        /// </summary>
        [ClassMember(
            Description =
                "TRUE means that only one unique (based on case insensitive comparision) value is expected to exist in the database. " +
                "Use this attribute for user account names for instance (like email addresses)",
            LongDescription =
                "See -" + nameof(BaseDatabase.TryAssertUniqueness) + "-. " +
                "A corresponding uniqueness index could also be created in the database")] // TODO: FUTURE DEVELOPEMENT, create such 
        public bool IsUniqueInDatabase { get; set; }
        public void AssertIsUniqueInDatabase() {
            if (!IsUniqueInDatabase) throw new BaseDatabase.UniquenessException("!" + nameof(IsUniqueInDatabase) + ". Details: " + ToString());
        }

        [ClassMember(Description = "Hint about not to expose actual value of Property as JSON / HTML, and to generate corresponding \"password\" input fields in HTML.")]
        public bool IsPassword { get; set; }
        public void AssertIsPassword(Func<string> detailer) {
            if (!IsPassword) throw new IsPasswordException(
                "Not marked as " + nameof(PropertyKeyAttribute) + "." + nameof(IsPassword) + "\r\n" +
                ToString() + "\r\n" +
                detailer.Result("\r\nDetails: ")
            );
        }
        public class IsPasswordException : ApplicationException {
            public IsPasswordException(string message) : base(message) { }
            public IsPasswordException(string message, Exception inner) : base(message, inner) { }
        }

        [ClassMember(
            Description =
                "TRUE if property has to exist for -" + nameof(Parents) + "-",
            LongDescription =
                "Used for -" + nameof(CoreAPIMethod.AddEntity) + "- (-" + nameof(APIMethodOrigin.Autogenerated) + "- -" + nameof(API.APIMethod) + ")- in order to construct necessary parameters. " +
                "Note that -" + nameof(IsMany) + "- combined with -" + nameof(IsObligatory) + "- will result in -" + nameof(PropertyKeyWithIndex.Index) + "-#1 being used")]
        public bool IsObligatory { get; set; }

        [ClassMember(Description = "Instructs -" + nameof(Property.Create) + "- to generate a -" + nameof(PropertyT<string>) + "- object if -" + nameof(PropertyKeyAttributeEnriched.TryValidateAndParse) + "- fails")]
        public bool IsNotStrict { get; set; }

        /// <summary>
        /// See <see cref="CoreP.AccessLevelRead"/> 
        /// Note strict default of <see cref="AccessLevel.System"/> 
        /// </summary>        
        public AccessLevel AccessLevelRead { get; set; } = AccessLevel.System;
        /// <summary>
        /// See <see cref="CoreP.AccessLevelWrite"/> 
        /// Note strict default of <see cref="AccessLevel.System"/> 
        /// </summary>
        public AccessLevel AccessLevelWrite { get; set; } = AccessLevel.System;

        /// <summary>
        /// The current <see cref="ConfigurationAttribute.Environment"/> has to be equivalent or lower in order for the property to be shown / accepted.
        /// </summary>
        public Environment Environment { get; set; } = Environment.Production;

        /// <summary>
        /// Hint about when we should read children from database (no need for SQL query if children
        /// are known never to exist)
        /// 
        /// TODO: Elaborate more on this. 
        /// </summary>
        public bool CanHaveChildren { get; set; }

        /// <summary>
        /// TODO: TURN INTO LIST, WITH CSS AS ENUM
        /// CSS class to be used when generating HTML representation of property
        /// 
        /// TODO: Elaborate more on this. 
        /// TODO: Maybe more classes even?
        /// 
        /// TODO: Or should we skip this property altogether and instead create an empty CSS-file with auto-generated class-names
        /// based on enum "class"-name and enum value? 
        /// </summary>
        public string CSSClass { get; set; }

        private bool _isMany;
        /// <summary>
        /// TODO: Implement as <see cref="CoreP"/> and move documentation there so can be made available via 
        /// TODO: <see cref="BaseAttribute.GetProperties"/>-mechanism in order to push this information out to the API interface.
        /// 
        /// Stored in memory in the <see cref="BaseEntity.Properties"/>-collection under
        /// one parent whose own <see cref="BaseEntity.Properties"/>-collection again 
        /// contains each instance with collection index corresponding to <see cref="int.MaxValue"/> minus index.
        /// 
        /// In the database they are stored with index #1, #2 like PhoneNumber#1, PhoneNumber#2 and so on.
        /// </summary>        
        [ClassMember(
            Description =
                "Signifies that several active current instances may exist " +
                "(like PhoneNumber#1, PhoneNumber#2 for a customer for instance)",
            LongDescription =
                "The -" + nameof(BaseDatabase) + "- implementation should be able to handle on-the-fly changes between TRUE and FALSE for this attribute. " +
                "Going from FALSE to TRUE should result in #1 being added to the relevant existing keys in the database, and " +
                "going from TRUE to FALSE should result in # being replaced by _ (resulting in PhoneNumber_1, PhoneNumber_2 and so on) " +
                "Note that -" + nameof(IsMany) + "- combined with -" + nameof(IsObligatory) + "- will result in -" + nameof(PropertyKeyWithIndex.Index) + "-#1 being used"
        )]
        public bool IsMany { get => _isMany; set { _isMany = value; _isManyIsSet = true; } }
        /// <summary>
        /// TODO: Expand on concept of <see cref="IsManyIsSet"/> in order to improve on <see cref="EnrichFrom"/>
        /// </summary>
        private bool _isManyIsSet = false;
        public bool IsManyIsSet => _isManyIsSet;

        /// <summary>
        /// </summary>
        /// <param name="detailer">May be null</param>
        public void AssertIsMany(Func<string> detailer) {
            if (!IsMany) throw new IsManyException(ToString() + detailer.Result("\r\nDetails: "));
        }

        public class IsManyException : ApplicationException {
            public IsManyException(string message) : base(message) { }
            public IsManyException(string message, Exception inner) : base(message, inner) { }
        }

        public void AssertNotIsMany(Func<string> detailer) {
            if (IsMany) throw new IsNotManyException(ToString() + detailer.Result("\r\nDetails: "));
        }

        public class IsNotManyException : ApplicationException {
            public IsNotManyException(string message) : base(message) { }
            public IsNotManyException(string message, Exception inner) : base(message, inner) { }
        }

        private bool _isExternal;
        /// <summary>
        /// TODO: Implement as <see cref="CoreP"/> and move documentation there so can be made available via 
        /// TODO: <see cref="BaseAttribute.GetProperties"/>-mechanism in order to push this information out to the API interface.
        /// </summary>
        [ClassMember(Description = "Denotes properties that originates from external systems through -" + nameof(BaseSynchronizer) + "-.")]
        public bool IsExternal { get => _isExternal; set { _isExternal = value; _isExternalIsSet = true; } }
        private bool _isExternalIsSet = false;
        public bool IsExternalIsSet => _isExternalIsSet;


        /// <summary>
        /// </summary>
        /// <param name="detailer">May be null</param>
        public void AssertIsExternal(Func<string> detailer) {
            if (!true.Equals(IsExternal)) throw new IsExternalException(ToString() + detailer.Result("\r\nDetails: "));
        }

        public class IsExternalException : ApplicationException {
            public IsExternalException(string message) : base(message) { }
            public IsExternalException(string message, Exception inner) : base(message, inner) { }
        }

        private bool _isInjected;
        /// <summary>
        /// TODO: Implement as <see cref="CoreP"/> and move documentation there so can be made available via 
        /// TODO: <see cref="BaseAttribute.GetProperties"/>-mechanism in order to push this information out to the API interface.
        /// 
        /// TODO: As of Sep 2017 this "flag" is not actually consumed in any sense, nor asserted set by any involved injection method.
        /// TODO: In other words, it is used for only informational purpose for the user / developer.
        /// </summary>
        [ClassMember(Description = "Denotes properties that are injected by either -" + nameof(BaseInjector) + "- or -" + nameof(BaseSynchronizer.Inject) + "-.")]
        public bool IsInjected { get => _isInjected; set { _isInjected = value; _isInjectedIsSet = true; } }
        private bool _isInjectedIsSet = false;
        public bool IsInjectedIsSet => _isInjectedIsSet;

        /// <summary>
        /// </summary>
        /// <param name="detailer">May be null</param>
        public void AssertIsInjected(Func<string> detailer) {
            if (!true.Equals(IsInjected)) throw new IsInjectedException(ToString() + detailer.Result("\r\nDetails: "));
        }

        public class IsInjectedException : ApplicationException {
            public IsInjectedException(string message) : base(message) { }
            public IsInjectedException(string message, Exception inner) : base(message, inner) { }
        }

        private bool _hasLimitedRange;
        /// <summary>
        /// TODO: Implement as <see cref="CoreP"/> and move documentation there so can be made available via 
        /// TODO: <see cref="BaseAttribute.GetProperties"/>-mechanism in order to push this information out to the API interface.
        /// 
        /// Note how <see cref="PropertyKeyAttributeEnriched.Initialize"/> will set this automatically for boolean and enum. 
        /// 
        /// Note how <see cref="BaseInjector.CalculateForeignKeyAggregates"/> actually sets this dynamically also.
        /// </summary>
        [ClassMember(
            Description =
                "Denotes that there is a limited range (limited in the practical sense) for values of this property, " +
                "making this property relevant for drill down suggestions",
            LongDescription =
                "Typical cut-off could be 20 for instance (since it is still practical to suggest 20 different drill-down suggestions).\r\n" +
                "For wider ranges the concept of -" + nameof(Percentile) + "- should be used"
        )]
        public bool HasLimitedRange { get => _hasLimitedRange; set { _hasLimitedRange = value; _hasLimitedRangeIsSet = true; } } // Note that allowed to set multiple times.
        private bool _hasLimitedRangeIsSet = false;
        public bool HasLimitedRangeIsSet => _hasLimitedRangeIsSet;

        /// <summary>
        /// </summary>
        /// <param name="detailer">May be null</param>
        public void AssertHasLimitedRange(Func<string> detailer) {
            if (!true.Equals(HasLimitedRange)) throw new HasLimitedRangeException(ToString() + detailer.Result("\r\nDetails: "));
        }

        public class HasLimitedRangeException : ApplicationException {
            public HasLimitedRangeException(string message) : base(message) { }
            public HasLimitedRangeException(string message, Exception inner) : base(message, inner) { }
        }

        /// <summary>
        /// TODO: Implement as <see cref="CoreP"/> and move documentation there so can be made available via 
        /// TODO: <see cref="BaseAttribute.GetProperties"/>-mechanism in order to push this information out to the API interface.
        /// 
        /// May be null. 
        /// 
        /// Note how <see cref="PropertyKeyAttributeEnriched.Initialize"/> will set this automatically (if not already set) 
        /// for known types like long, double, boolean, enum and DateTime,
        /// (and also set it to <see cref="Operator.EQ"/> for <see cref="HasLimitedRange"/>)
        /// </summary>
        [ClassMember(Description = "Operators suitable for use against this property.")]
        public Operator[] Operators;

        private HashSet<Operator> _operatorsAsHashSet;
        public HashSet<Operator> OperatorsAsHashSet => _operatorsAsHashSet ?? (_operatorsAsHashSet = new Func<HashSet<Operator>>(() => {
            var retval = new HashSet<Operator>();
            if (Operators == null) return retval;
            Operators.ForEach(o => retval.Add(o));
            return retval;
        })());

        /// <summary>
        /// Implies both
        /// 1) That the type given also belongs in <see cref="Parents"/> and
        /// 2) <see cref="IsExternal"/> 
        /// (in other words, you do not have to specify neither <see cref="Parents"/> nor <see cref="IsExternal"/>).
        /// See <see cref="PropertyKeyAttributeEnriched.Initialize"/> for details. 
        /// </summary>
        [ClassMember(
            Description = "External id (in contrast to AgoRapide internal id -" + nameof(CoreP.DBId) + "-). ",
            LongDescription = "Used to link together data from external sources."
        )]
        public Type ExternalPrimaryKeyOf;

        /// <summary>
        /// If <see cref="Type"/> is not given then it will automatically be set to <see cref="long"/> (long) by <see cref="PropertyKeyAttributeEnriched.Initialize"/>.
        /// </summary>
        [ClassMember(
            Description = "Indicates that value (as AgoRapide -" + nameof(DBField.id) + "-) points to related entity.")]
        public Type ForeignKeyOf;

        /// <summary>
        /// Implies <see cref="IsExternal"/> (in other words, you do not have to specify <see cref="IsExternal"/>).
        /// If <see cref="Type"/> is not given then it will automatically be set to <see cref="long"/> (long) by <see cref="PropertyKeyAttributeEnriched.Initialize"/>.
        /// </summary>
        [ClassMember(
            Description = "Indicates that value (as some external system id) points to related entity.",
            LongDescription = "Basis for generating -" + nameof(ForeignKeyOf) + "- when linking together data from external sources.")]
        public Type ExternalForeignKeyOf;

        private bool _isDocumentation;
        /// <summary>
        /// TODO: Implement as <see cref="CoreP"/> and move documentation there so can be made available via 
        /// TODO: <see cref="BaseAttribute.GetProperties"/>-mechanism in order to push this information out to the API interface.
        /// </summary>
        [ClassMember(
            Description =
                "Signifies that value may contain keys on the form -xxx- " +
                "which should be replaced with respective links by -" + nameof(Documentator.ReplaceKeys) + "-.",
            LongDescription =
                "This actual description (that you read now) is for instance marked as " + nameof(IsDocumentation) + " (see " + nameof(CoreP.LongDescription) + ").\r\n" +
                "Especially useful when making HTML representations of properties (see -" + nameof(Property.HTML) + "-)."
        )]
        public bool IsDocumentation { get => _isDocumentation; set { _isDocumentation = value; _isDocumentationIsSet = true; } }
        private bool _isDocumentationIsSet = false;
        private bool IsDocumentationIsSet => _isDocumentationIsSet;

        /// <summary>
        /// May be null. 
        /// 
        /// Set this to the type of a class inheriting <see cref="IGroupDescriber"/>
        /// </summary>
        [ClassMember(Description = "Practical mechanism for describing properties with common properties through -" + nameof(IGroupDescriber) + "-")]
        public Type Group { get; set; }

        /// <summary>
        /// Will always be set by <see cref="PropertyKeyAttributeEnriched.Initialize"/> if not given.
        /// </summary>
        [ClassMember(Description = "List of aggregations desired for -" + nameof(Type) + "- like -" + nameof(AggregationType.Count) + "- or -" + nameof(AggregationType.Sum) + "-.")]
        public AggregationType[] AggregationTypes;

        /// <summary>
        /// Only relevant when <see cref="Type"/> is <see cref="DateTime"/>
        /// </summary>
        public DateTimeFormat DateTimeFormat { get; set; }

        /// <summary>
        /// Describes entities for which this property is used.
        /// 
        /// Typical example for an enum like P would be:
        /// public enum P {
        ///   ...
        ///   [AgoRapide(Parents = new Type[] { typeof(Person) }, Type = typeof(DateTime))]
        ///   DateOfBirth
        ///   ...
        /// }
        ///   
        /// Note that once <see cref="Parents"/> have been set for a given property then you will have to also add
        /// <see cref="AccessLevelRead"/> (and <see cref="AccessLevelWrite"/>) 
        /// both for the property AND (IMPORTANT!) ALSO for the entity class itself because 
        /// <see cref="BaseEntity.GetExistingProperties"/> (through <see cref="Extensions.GetChildPropertiesForAccessLevel"/>) 
        /// will take into account these access rights (which default to <see cref="AccessLevel.System"/>. 
        /// </summary>
        public Type[] Parents { get; set; }

        /// <summary>
        /// TODO: Implement so that may also be given for Type string (string will be converted to int, and checked for value. Useful for postal codes)
        /// 
        /// TODO: Not implemented in <see cref="PropertyKeyAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public double MinValueDbl { get; set; }
        /// <summary>
        /// TODO: Implement so that may also be given for Type string (string will be converted to int, and checked for value. Useful for postal codes)
        /// 
        /// TODO: Not implemented in <see cref="PropertyKeyAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public double MaxValueDbl { get; set; }

        /// <summary>
        /// TODO: Not implemented in <see cref="PropertyKeyAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public DateTime MinValueDtm { get; set; }
        /// <summary>
        /// TODO: Not implemented in <see cref="PropertyKeyAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public DateTime MaxValueDtm { get; set; }

        /// <summary>
        /// Only relevant for Type string
        /// TODO: Not implemented in <see cref="PropertyKeyAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public long MinLength { get; set; }
        /// <summary>
        /// Only relevant for Type string
        /// TODO: Not implemented in <see cref="PropertyKeyAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public long MaxLength { get; set; }

        /// <summary>
        /// TODO: Not implemented in <see cref="PropertyKeyAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public string RegExpValidator { get; set; }

        /// <summary>
        /// TODO: Consider introducing a constant useful for <see cref="RegExpValidator"/>, eliminating this property.
        /// </summary>
        [ClassMember(
            Description =
                "TRUE indicates that the value given must be a valid C# identifier. " +
                "Especially used for -" + nameof(CoreP.QueryId) + "-. " +
                "Only allowed for -" + nameof(Type) + "- string",
            LongDescription =
                "A practical consequence will be that the value can also be used in a HTTP GET query-string without escaping)")]
        public bool MustBeValidCSharpIdentifier { get; set; }

        /// <summary>
        /// Default value when used as parameter in API method
        /// 
        /// TODO: Is should be possible to have different rules for default for different APIMethods.
        /// TODO: A given property may be required in one context, but accepted with default value in
        /// TODO: other cases.
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Note that will be set automatically through <see cref="AgoRapideAttributeT(PropertyKeyAttribute)"/> for 
        /// <see cref="PropertyKeyAttribute"/> having <see cref="PropertyKeyAttribute.Type"/> which is <see cref="Type.IsEnum"/> 
        /// 
        /// TODO: Add to <see cref="PropertyKeyAttribute.ValidValues"/> a List of tuples with description for each value
        /// TODO: (this is needed for HTML SELECT tags)
        /// </summary>
        /// <returns></returns>
        [ClassMember(Description = "If set then the values contained are the ONLY values accepted.")]
        public string[] ValidValues { get; set; }

        [ClassMember(Description = "Used for unit testing in order to assert failure of validation.")]
        public string[] InvalidValues { get; set; }

        /// <summary>
        /// <see cref="ValidValues"/> will usually be copied to <see cref="SampleValues"/> if <see cref="SampleValues"/> is not set.
        /// </summary>
        [ClassMember(Description = "Used for\r\n" +
            "1) Unit testing in order to assert validation and\r\n" +
            "2) giving sample values for API calls."
        )]
        public string[] SampleValues { get; set; }

        /// <summary>
        /// Returns <see cref="PropertyKeyAttribute"/> for given <paramref name="_enum"/>-value.
        /// 
        /// Normally called from <see cref="PropertyKeyMapper.MapEnum{T}"/> but may also be called from 
        /// <see cref="EnumValueAttribute.GetAttribute"/>- 
        /// </summary>
        /// <param name="_enum"></param>
        /// <returns></returns>
        public static new PropertyKeyAttribute GetAttribute(object _enum) {
            // TODO: Consider moving more of this code into AgoRapideAttribute-class
            var type = _enum.GetType();
            NotOfTypeEnumException.AssertEnum(type); // TODO: Necessary? Most possibly YES!
            if (type.GetEnumAttribute().AgoRapideEnumType != EnumType.PropertyKey) throw new InvalidEnumException(type.GetEnumAttribute().AgoRapideEnumType,
                nameof(EnumType) + "." + EnumType.PropertyKey + " required here,\r\n" +
                "found " + type.GetEnumAttribute().AgoRapideEnumType + ".\r\n" +
                "Possible resolution (assuming that " + _enum + " really is used to describe entity properties):\r\n" +
                "Add\r\n" +
                "[" + nameof(EnumAttribute) + "(" + nameof(EnumAttribute.AgoRapideEnumType) + " = " + nameof(EnumType) + "." + EnumType.PropertyKey + ")]\r\n" +
                "to declaration of " + _enum.GetType());
            var field = type.GetField(_enum.ToString());
            if (field == null) { /// Added 5 Jun 2017. Needed for instance when called from <see cref="Extensions.GetValue2{TKey, TValue}"/>
                if (type.Equals(typeof(CoreP)) && PropertyKeyMapper.TryGetA((CoreP)_enum, out var retval)) return retval.Key.A;
                throw new NullReferenceException(nameof(type.GetField) + "(): Cause: " + type + "." + _enum.ToString() + " is most probably not defined.");
            } else {
                var retval = GetAttributeThroughFieldInfo<PropertyKeyAttribute>(field, () => type + "." + _enum);
                retval._enumValue = _enum;
                retval._enumValueExplained = retval.EnumValue.GetType() + "." + _enum.ToString();
                return retval;
            }
        }

        /// <summary>
        /// Only to be used by <see cref="PropertyKeyAttributeEnriched.Initialize"/>
        /// 
        /// . TODO: Make private or similar.
        /// 
        /// Enriches non-set properties of this attribute class with properties from other attribute class
        /// 
        /// Note how:
        /// 1) Boolean values are only transferred if they are TRUE at <paramref name="other"/>-end.
        ///    TODO: See <see cref="IsManyIsSet"/> for how to correct on this.
        /// 2) Enum values are only transferred if they are not default at this end.
        /// 3) Other value types are typically not transferred
        ///    TODO: See <see cref="IsManyIsSet"/> for how to correct on this.
        /// </summary>
        /// <param name="other">
        /// </param>
        public void EnrichFrom(PropertyKeyAttribute other) {
            // TODO: Constantly ensure that all properties are included here (Jan 2017)
            if (!TypeIsSet && other.TypeIsSet) Type = other.Type;

            if (AggregationTypes == null) {
                AggregationTypes = other.AggregationTypes;
            } else if (other.AggregationTypes == null) { // No changes
            } else { // Merge both lists
                var temp = AggregationTypes.ToList();
                temp.AddRange(other.AggregationTypes.Where(o => !temp.Any(p => p == o)).ToList());
                AggregationTypes = temp.ToArray();
            }

            if (Parents == null) {
                Parents = other.Parents;
            } else if (other.Parents == null) { // No changes
            } else { // Merge both lists
                var temp = Parents.ToList();
                temp.AddRange(other.Parents.Where(o => !temp.Any(p => p == o)).ToList());
                Parents = temp.ToArray();
            }

            if (string.IsNullOrEmpty(RegExpValidator)) RegExpValidator = other.RegExpValidator;
            if (string.IsNullOrEmpty(DefaultValue)) DefaultValue = other.DefaultValue;
            if (ValidValues == null) ValidValues = other.ValidValues;
            if (InvalidValues == null) InvalidValues = other.InvalidValues;
            if (SampleValues == null) SampleValues = other.SampleValues;
            if (Operators == null) Operators = other.Operators;

            if (string.IsNullOrEmpty(Description)) {
                Description = other.Description;
            } else {
                Description += (Description.EndsWith(".") ? "" : ".") + "\r\nCore " + nameof(other.Description) + ": " + other.Description;
            }
            if (string.IsNullOrEmpty(LongDescription)) {
                LongDescription = other.LongDescription;
            } else {
                LongDescription += (LongDescription.EndsWith(".") ? "" : ".") + "\r\nCore " + nameof(other.LongDescription) + ": " + other.LongDescription;
            }

            // TODO: Should CSSClass also be merged together?
            if (string.IsNullOrEmpty(CSSClass)) CSSClass = other.CSSClass;

            if (AccessLevelRead == AccessLevel.System) AccessLevelRead = other.AccessLevelRead; // Careful with what is default value here
            if (AccessLevelWrite == AccessLevel.System) AccessLevelWrite = other.AccessLevelWrite; // Careful with what is default value here

            // Value types are more difficult to enrich 

            /// TODO: See prototype solution with <see cref="IsManyIsSet"/>
            /// TODO:
            // -----------------
            // We would have liked to use nullable versions but they are not "valid attribute parameter type"
            // In order words, something like this is not possible:
            // -----------------
            //if (MinLength == null) MinLength = other.MinLength;
            //if (MaxLength == null) MaxLength = other.MaxLength;
            //if (MinValueDbl == null) MinValueDbl = other.MinValueDbl;
            //if (MaxValueDbl == null) MaxValueDbl = other.MaxValueDbl;
            //if (MinValueDtm == null) MinValueDtm = other.MinValueDtm;
            //if (MaxValueDtm == null) MaxValueDtm = other.MaxValueDtm;
            // -----------------

            if (PriorityOrder == 0) PriorityOrder = other.PriorityOrder;

            // Note how boolean values are only transferred if they are TRUE.

            /// TODO: See prototype solution with <see cref="IsManyIsSet"/>
            /// TODO:
            /// TODO: For <see cref="PropertyKeyAttribute"/> we could use a boolean-enum of None, True and False. 
            /// TODO: for bool attributes since bool? is not allowed. 
            /// TODO: THIS COULD SOLVE THE DILEMMA OF OVERWRITING FALSE WITH TRUE IN <see cref="PropertyKeyAttribute.EnrichFrom"/>
            /// TODO: This idea could also be related to the idea of making an immutable copy of <see cref="PropertyKeyAttribute"/>
            /// TODO: (this copy could have user-friendly bool-values instead of an enum)
            /// TODO: (the whole change could be without breaking code at the client side)
            // if (other.IsDefault) SetAsDefault(); // Removed 8 Mar 2017

            // Not allowed since no rationale seen, and it only seems to add complexity
            if (other.IsExternalIsSet) throw new IsExternalException(nameof(IsExternalIsSet) + " is illegal for " + System.Reflection.MethodBase.GetCurrentMethod().Name + ".\r\nDetails:\r\nThis: " + ToString() + "\r\n" + nameof(other) + ":" + other.ToString());
            if (other.ExternalPrimaryKeyOf != null) throw new IsExternalException(nameof(ExternalPrimaryKeyOf) + " is illegal for " + System.Reflection.MethodBase.GetCurrentMethod().Name + ".\r\nDetails:\r\nThis: " + ToString() + "\r\n" + nameof(other) + ":" + other.ToString());

            /// TODO: Expand on concept of <see cref="IsManyIsSet"/> in order to improve on <see cref="EnrichFrom"/>
            if (other.IsManyIsSet) IsMany = other.IsMany;
            if (other.IsDocumentationIsSet) IsDocumentation = other.IsDocumentation;
            if (other.HasLimitedRange) HasLimitedRange = other.HasLimitedRange;

            /// TODO: Expand on concept of <see cref="IsManyIsSet"/> in order to improve on <see cref="EnrichFrom"/>
            if (other.IsObligatory) IsObligatory = other.IsObligatory;
            /// TODO: Expand on concept of <see cref="IsManyIsSet"/> in order to improve on <see cref="EnrichFrom"/>
            if (other.IsNotStrict) IsNotStrict = other.IsNotStrict;
            /// TODO: Expand on concept of <see cref="IsManyIsSet"/> in order to improve on <see cref="EnrichFrom"/>
            if (other.IsUniqueInDatabase) IsUniqueInDatabase = other.IsUniqueInDatabase;
            /// TODO: Expand on concept of <see cref="IsManyIsSet"/> in order to improve on <see cref="EnrichFrom"/>
            if (other.IsPassword) IsPassword = other.IsPassword;
            /// TODO: Expand on concept of <see cref="IsManyIsSet"/> in order to improve on <see cref="EnrichFrom"/>
            if (other.CanHaveChildren) CanHaveChildren = other.CanHaveChildren;

            if (DateTimeFormat == DateTimeFormat.None) DateTimeFormat = other.DateTimeFormat;
        }

        /// <summary>
        /// --------------------------
        /// Note how we DO NOT set any <see cref="PropertyKeyAttribute.Description"/> for <see cref="CoreP.CoreAPIMethod"/> 
        /// but instead rely on the <see cref="ClassAttribute.Description"/> set here. 
        /// This comment describes the recommended approach to setting attributes when the type given (<see cref="PropertyKeyAttribute.Type"/>) 
        /// is one of your own classes / enums, or one of the AgoRapide classes / enums 
        /// --------------------------
        /// </summary>
        /// <param name="other"></param>
        public void EnrichFrom(BaseAttribute other) {
            if (string.IsNullOrEmpty(Description)) {
                Description = other.Description;
            } else {
                Description += (Description.EndsWith(".") ? "" : ".") + "\r\nCore " + nameof(other.Description) + ": " + other.Description;
            }
            if (string.IsNullOrEmpty(LongDescription)) {
                LongDescription = other.LongDescription;
            } else {
                LongDescription += (LongDescription.EndsWith(".") ? "" : ".") + "\r\nCore " + nameof(other.LongDescription) + ": " + other.LongDescription;
            }
            switch (other) {
                case ClassAttribute otherAsClassAttribute:
                    if (InvalidValues == null) InvalidValues = otherAsClassAttribute.InvalidValues;
                    if (SampleValues == null) SampleValues = otherAsClassAttribute.SampleValues; break;
            }
        }

        /// Inherited implementations from <see cref="EnumValueAttribute"/> are sufficient, no need to override here:
        //public override string ToString() => nameof(EnumValue) + ": " + (_enumValue?.ToString() ?? "[NULL]") + "\r\n" + base.ToString();
        //protected override string GetIdentifier() => GetType().ToStringShort().Replace("Attribute", "") + "_" + EnumValue.GetType().ToStringShort() + "_" + EnumValue.ToString();

    }
}