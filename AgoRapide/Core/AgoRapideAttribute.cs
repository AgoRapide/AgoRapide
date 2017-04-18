﻿using System;
using System.Linq;
using AgoRapide;
using System.Collections.Generic;
using System.ComponentModel;
using AgoRapide.Database;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: Use more concept like <see cref="IsManyIsSet"/> in order for <see cref="EnrichFrom"/> to know
    /// TODO: which values to enrich.
    /// 
    /// Usually accessed through <see cref="Extensions.GetAgoRapideAttributeT{T}(T)"/> which returns the more refined class
    /// <see cref="AgoRapideAttributeT"/>-class which again contains this class as a member.
    /// </summary>
    [Enum(Description = "Describes an enum member (enum value) of type -" + nameof(EnumType.EntityPropertyEnum) + "-. Super class -" + nameof(EnumMemberAttribute) + "- describes enum values NOT of type -" + nameof(EnumType.EntityPropertyEnum) + "-")]
    public class AgoRapideAttribute : ClassMemberAttribute {

        /// <summary>
        /// Default empty constructor for all instances when originates from C# code
        /// </summary>
        public AgoRapideAttribute() { }

        /// <summary>
        /// Constructor for when originates from database (See <see cref="EnumMapper.TryAddA"/>)
        /// </summary>
        public AgoRapideAttribute(
            string property,
            string description,
            string longDescription,
            bool isMany) {

            _property = property;
            Description = description;
            LongDescription = longDescription;
            IsMany = isMany;
        }

        private object _property;
        /// <summary>
        /// The actual property (actual enum-value) which we are an attribute for.
        /// 
        /// Only relevant when attribute for an enum-value. 
        /// TODO: SPLIT <see cref="AgoRapideAttribute"/> into EnumAttribute and ClassAttribute.
        /// 
        /// Note that for dynamically originated attributes (see <see cref="AgoRapideAttributeEnrichedDyn"/>) this
        /// value will actually be a string, not an <see cref="Enum"/>. 
        /// </summary>
        public object Property => _property ?? throw new NullReferenceException(nameof(_property) + ".\r\nDetails: " + ToString());

        public void AssertProperty() {
            if (Property == null) throw new NullReferenceException(nameof(Property) + " for " + ToString());
        }

        [ClassMember(Description = "The underlying (more closer to the core AgoRapide library) property that -" + nameof(AgoRapideAttributeEnriched) + "- will inherit values from.",
            LongDescription =
                "At the same time attributes for that property will be overridden by this -" + nameof(AgoRapideAttribute) + "- " +
                "(conceptual similar to virtual overridden C# properties). " +
                "The value will often correspond to a -" + nameof(CoreP) + "- value")]
        public object InheritAndEnrichFromProperty { get; set; }

        public PriorityOrder PriorityOrder { get; set; }

        /// <summary>
        /// TODO: Implement automatic creation of uniqueness index in database in Startup.cs
        /// </summary>
        [AgoRapide(
            Description =
                "TRUE means that only one unique (based on case insensitive comparision) value is expected to exist in the database. " +
                "Use this attribute for user account names for instance (like email addresses)",
            LongDescription =
                "See -" + nameof(IDatabase.TryAssertUniqueness) + "-. " +
                "A corresponding uniqueness index could also be created in the database")] // TODO: FUTURE DEVELOPEMENT, create such 
        public bool IsUniqueInDatabase { get; set; }
        public void AssertIsUniqueInDatabase() {
            if (!IsUniqueInDatabase) throw new UniquenessException("!" + nameof(IsUniqueInDatabase) + ". Details: " + ToString());
        }

        [ClassMember(Description = "Hint about not to expose actual value of Property as JSON / HTML, and to generate corresponding \"password\" input fields in HTML.")]
        public bool IsPassword { get; set; }

        [ClassMember(
            Description =
                "TRUE if property has to exist for -" + nameof(Parents) + "-",
            LongDescription =
                "Used for -" + nameof(CoreMethod.AddEntity) + "- (-" + nameof(APIMethodOrigin.Autogenerated) + "- -" + nameof(APIMethod) + ")- in order to construct necessary parameters. " +
                "Note that -" + nameof(IsMany) + "- combined with -" + nameof(IsObligatory) + "- will result in -" + nameof(PropertyKey.Index) + "-#1 being used")]
        public bool IsObligatory { get; set; }

        [ClassMember(Description = "Instructs -" + nameof(AgoRapide.Property.Create) + "- to generate a -" + nameof(PropertyT<string>) + "- object if -" + nameof(AgoRapideAttributeEnriched.TryValidateAndParse) + "- fails")]
        public bool IsNotStrict { get; set; }

        /// <summary>
        /// Only relevant when <see cref="Type"/> is <see cref="DateTime"/>
        /// </summary>
        public DateTimeFormat DateTimeFormat { get; set; }

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
        /// The current <see cref="Environment"/> has to be equivalent or lower in order for the property to be shown / accepted.
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
        /// Stored in memory in the <see cref="BaseEntity.Properties"/>-collection under
        /// one parent whose own <see cref="BaseEntity.Properties"/>-collection again 
        /// contains each instance with collection index corresponding to <see cref="int.MaxValue"/> minus index.
        /// 
        /// In the database they are stored with index #1, #2 like PhoneNumber#1, PhoneNumber#2 and so on.
        /// </summary>        
        [AgoRapide(
            Description = "Signifies that several active current instances may exist (like PhoneNumber#1, PhoneNumber#2 for a customer for instance)",
            LongDescription =
                "The -" + nameof(IDatabase) + "- implementation should be able to handle on-the-fly changes between TRUE and FALSE for this attribute. " +
                "Going from FALSE to TRUE should result in #1 being added to the relevant existing keys in the database, and " +
                "going from TRUE to FALSE should result in # being replaced by _ (resulting in PhoneNumber_1, PhoneNumber_2 and so on) " +
                "Note that -" + nameof(IsMany) + "- combined with -" + nameof(IsObligatory) + "- will result in -" + nameof(PropertyKey.Index) + "-#1 being used"
            )]
        public bool IsMany { get => _isMany; set { _isMany = value; _isManyIsSet = true; } }

        /// <summary>
        /// TODO: Expand on concept of <see cref="IsManyIsSet"/> in order to improve on <see cref="EnrichFrom"/>
        /// </summary>
        private bool _isManyIsSet = false;
        private bool IsManyIsSet => _isManyIsSet;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="detailer">May be null</param>
        public void AssertIsMany(Func<string> detailer) {
            if (!true.Equals(IsMany)) throw new IsManyException(ToString() + detailer.Result("\r\nDetails: "));
        }

        public class IsManyException : ApplicationException {
            public IsManyException(string message) : base(message) { }
            public IsManyException(string message, Exception inner) : base(message, inner) { }
        }

        /// <summary>
        /// May be null. 
        /// 
        /// Set this to the type of a class inheriting <see cref="IGroupDescriber"/>
        /// </summary>
        [ClassMember(Description = "Practical mechanism for describing properties with common properties through -" + nameof(IGroupDescriber) + "-")]
        public Type Group { get; set; }

        private Type _type;
        [AgoRapide(
            Description = "The type of this Property.",
            LongDescription =
                "Typical examples are\r\n" +
                "1) typeof(string), typeof(long), typeof(DateTime),\r\n" +
                "2) typeof(CoreMethod) / typeof(AnyEnum),\r\n" +
                "3) Can also be a type assignable to -" + nameof(ITypeDescriber) + ",\r\n" +
                "4) or any type understood by " + nameof(AgoRapideAttributeEnriched) + "\r\n" +
                "\r\n" +
                "Will be set to string by " + nameof(AgoRapideAttributeEnriched) + " if not given."
            )]
        public Type Type { get => _type ?? throw new NullReferenceException(nameof(Type) + ". Supposed to always be set from " + nameof(AgoRapideAttributeEnriched) + ".\r\nDetails: " + ToString()); set => _type = value; }
        public bool TypeIsSet => _type != null;

        /// <summary>
        /// Describes entities for which this property is used.
        /// 
        /// Typical example for a  enum like P would be:
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
        /// TODO: Not implemented in <see cref="AgoRapideAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public double MinValueDbl { get; set; }
        /// <summary>
        /// TODO: Implement so that may also be given for Type string (string will be converted to int, and checked for value. Useful for postal codes)
        /// 
        /// TODO: Not implemented in <see cref="AgoRapideAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public double MaxValueDbl { get; set; }

        /// <summary>
        /// TODO: Not implemented in <see cref="AgoRapideAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public DateTime MinValueDtm { get; set; }
        /// <summary>
        /// TODO: Not implemented in <see cref="AgoRapideAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public DateTime MaxValueDtm { get; set; }

        /// <summary>
        /// Only relevant for Type string
        /// TODO: Not implemented in <see cref="AgoRapideAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public long MinLength { get; set; }
        /// <summary>
        /// Only relevant for Type string
        /// TODO: Not implemented in <see cref="AgoRapideAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public long MaxLength { get; set; }

        /// <summary>
        /// TODO: Not implemented in <see cref="AgoRapideAttributeEnriched.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public string RegExpValidator { get; set; }

        /// <summary>
        /// Default value when used as parameter in API method
        /// 
        /// TODO: Is should be possible to have different rules for default for different APIMethods.
        /// TODO: A given property may be required in one context, but accepted with default value in
        /// TODO: other cases.
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// If set then the values contained are the ONLY values accepted.
        /// 
        /// Note that will be set automatically through <see cref="AgoRapideAttributeT(AgoRapideAttribute)"/> for 
        /// <see cref="AgoRapideAttribute"/> having <see cref="AgoRapideAttribute.Type"/> which is <see cref="Type.IsEnum"/> 
        /// 
        /// TODO: Add to <see cref="AgoRapideAttribute.ValidValues"/> a List of tuples with description for each value
        /// TODO: (this is needed for HTML SELECT tags)
        /// </summary>
        /// <returns></returns>
        public string[] ValidValues { get; set; }

        /// <summary>
        /// Used for unit testing in order to assert failure of validation
        /// </summary>
        public string[] InvalidValues { get; set; }

        /// <summary>
        /// Used for
        /// 1) Unit testing in order to assert validation and 
        /// 2) giving sample values for API calls. 
        /// <see cref="ValidValues"/> will usually be copied to <see cref="SampleValues"/> if <see cref="SampleValues"/> is not set.
        /// </summary>
        public string[] SampleValues { get; set; }

        /// <summary>
        /// Only relevant when attribute for an enum. 
        /// TODO: SPLIT <see cref="AgoRapideAttribute"/> into EnumAttribute and ClassAttribute.
        /// </summary>
        public EnumType EnumType { get; set; }

        //public class NoAttributesDefinedException : ApplicationException {
        //    public NoAttributesDefinedException(string message) : base(message) { }
        //    public NoAttributesDefinedException(string message, Exception inner) : base(message, inner) { }
        //}

        /// <summary>
        /// TODO: SPLIT <see cref="AgoRapideAttribute"/> into EnumAttribute and ClassAttribute.
        /// 
        /// TODO: Fix this for other than KeyAttribute.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => "Enum: " + (_property?.ToString() ?? "[NULL]") + "\r\nDescription:\r\n" + Description + "\r\nLongDescription:\r\n" + LongDescription;

        ///// <summary>
        ///// Typically used by for instance <see cref= "GetAgoRapideAttribute" /> when no attribute found.
        ///// </summary>
        ///// <returns></returns>
        //public static AgoRapideAttribute GetNewDefaultInstance() => new AgoRapideAttribute { IsDefault = true };

        /// <summary>
        /// TOOD: ------------
        /// TODO: Do we want to limit this to enums? 
        /// TODO: See assert
        /// TODO: NotOfTypeEnumException.AssertEnum(type);
        /// TOOD: ------------
        /// 
        /// Returns <see cref="AgoRapideAttribute"/> for given <paramref name="_enum"/>-value.
        /// 
        /// Note that separate instances with <see cref="IsDefault"/> = True will be created 
        /// for every enum for which no <see cref="AgoRapideAttribute"/> is defined.             
        /// 
        /// Usually called from <see cref="Extensions.GetAgoRapideAttributeT{T}(T)"/> / <see cref="Extensions.GetAgoRapideAttribute(object)"/>. 
        /// </summary>
        /// <param name="_enum"></param>
        /// <returns></returns>
        public static AgoRapideAttribute GetAgoRapideAttribute(object _enum) {
            // TODO: Consider moving more of this code into AgoRapideAttribute-class
            var type = _enum.GetType();
            NotOfTypeEnumException.AssertEnum(type); // TODO: Necessary? Most possibly YES!
            if (type.GetEnumAttribute().EnumTypeY != EnumType.EntityPropertyEnum) throw new InvalidObjectTypeException(_enum, EnumType.EntityPropertyEnum + " required here");
            var field = type.GetField(_enum.ToString()) ?? throw new NullReferenceException(nameof(type.GetField) + "(): Cause: " + type.ToStringShort() + "." + _enum.ToString() + " is most probably not defined.");

            var retval = new Func<AgoRapideAttribute>(() => {
                var attributes = field.GetCustomAttributes(typeof(AgoRapideAttribute), true);
                switch (attributes.Length) {
                    case 0:
                        /// TODO: Duplicate code!
                        var tester = new Action<Type>(t => {
                            object found = field.GetCustomAttributes(t, true);
                            if (found != null) throw new IncorrectAttributeTypeUsedException(found, typeof(EnumMemberAttribute), type + "." + _enum);
                        });
                        tester(typeof(ClassAttribute));
                        tester(typeof(ClassMemberAttribute));
                        tester(typeof(EnumAttribute));
                        tester(typeof(EnumMemberAttribute));
                        // tester(typeof(AgoRapideAttribute));
                        return new AgoRapideAttribute { IsDefault = true };
                    case 1:
                        return (AgoRapideAttribute)attributes[0];
                    default:
                        throw new AttributeException(nameof(attributes) + ".Length > 1 (" + attributes.Length + ") for " + type.ToStringVeryShort() + "." + _enum.ToString());
                }
            })();
            retval._property = _enum;
            return retval;
        }

        /// <summary>
        /// Only to be used by <see cref="AgoRapideAttributeEnriched.Initialize"/>
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
        public void EnrichFrom(AgoRapideAttribute other) {
            // TODO: Constantly ensure that all properties are included here (Jan 2017)
            if (!TypeIsSet && other.TypeIsSet) Type = other.Type;

            if (Parents == null) {
                Parents = other.Parents;
            } else if (other.Parents == null) { // No changes
            } else { // Merge both lists
                var temp = Parents.ToList();
                temp.AddRange(other.Parents.ToList());
                Parents = temp.ToArray();
            }

            if (string.IsNullOrEmpty(RegExpValidator)) RegExpValidator = other.RegExpValidator;
            if (string.IsNullOrEmpty(DefaultValue)) DefaultValue = other.DefaultValue;
            if (ValidValues == null) ValidValues = other.ValidValues;
            if (InvalidValues == null) InvalidValues = other.InvalidValues;
            if (SampleValues == null) SampleValues = other.SampleValues;

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
            /// TODO: For <see cref="AgoRapideAttribute"/> we could use a boolean-enum of None, True and False. 
            /// TODO: for bool attributes since bool? is not allowed. 
            /// TODO: THIS COULD SOLVE THE DILEMMA OF OVERWRITING FALSE WITH TRUE IN <see cref="AgoRapideAttribute.EnrichFrom"/>
            /// TODO: This idea could also be related to the idea of making an immutable copy of <see cref="AgoRapideAttribute"/>
            /// TODO: (this copy could have user-friendly bool-values instead of an enum)
            /// TODO: (the whole change could be without breaking code at the client side)

            // if (other.IsDefault) SetAsDefault(); // Removed 8 Mar 2017

            /// TODO: See prototype solution with <see cref="IsManyIsSet"/>
            /// TODO:
            if (other.IsObligatory) IsObligatory = other.IsObligatory;
            if (other.IsNotStrict) IsNotStrict = other.IsNotStrict;
            if (other.IsUniqueInDatabase) IsUniqueInDatabase = other.IsUniqueInDatabase;
            if (other.IsPassword) IsPassword = other.IsPassword;
            if (other.CanHaveChildren) CanHaveChildren = other.CanHaveChildren;
            if (other.IsMany) IsMany = other.IsMany;

            if (DateTimeFormat == DateTimeFormat.None) DateTimeFormat = other.DateTimeFormat;
        }

        /// <summary>
        /// This method facilitates the following:
        /// --------------------------
        /// Note how we DO NOT set any <see cref="AgoRapideAttribute.Description"/> for <see cref="CoreP.CoreMethod"/> 
        /// but instead rely on the <see cref="ClassAttribute.Description"/> set here. 
        /// This comment describes the recommended approach to setting attributes when the type given (<see cref="AgoRapideAttribute.Type"/>) 
        /// is one of your own classes / enums, or one of the AgoRapide classes / enums 
        /// --------------------------
        /// </summary>
        /// <param name="other"></param>
        public void EnrichFrom(ClassAttribute other) {
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
        }
    }
}
