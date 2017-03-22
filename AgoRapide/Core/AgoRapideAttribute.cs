using System;
using System.Linq;
using AgoRapide;
using System.Collections.Generic;
using System.ComponentModel;
using AgoRapide.Database;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: Use <see cref="AgoRapideAttribute"/> itself on this class in order to describe the different
    /// TODO: elements instead of using XML comments.
    /// 
    /// TODO: SPLIT <see cref="AgoRapideAttribute"/> into EnumAttribute and ClassAttribute.
    /// 
    /// Use for describing either
    /// 
    /// 1) (most common) actual enum values.
    ///    Usually accessed through <see cref="Extensions.GetAgoRapideAttribute{T}(T)"/> which returns the more refined class
    ///    <see cref="AgoRapideAttributeT{TProperty}"/>-class which again contains this class as a member.
    ///    
    ///    Values are populated directly from the C# code using <see cref="System.ComponentModel"/> like
    ///       [Description("Example")] 
    ///       [AgoRapideAttribute(EntityType = typeof(string), LongDescription = "Long description")]
    ///       ActualEnumValue
    ///       
    /// or
    /// 
    /// 2) (less common) enum-"classes". 
    ///    (instead of XML-comments like this). 
    ///    See example for how this has been done for <see cref="CoreMethod"/>). 
    ///    Usually accessed through <see cref="Extensions.GetAgoRapideAttribute(Type)"/>. 
    ///    Some of the properties for <see cref="AgoRapideAttribute"/> are not relevant in this case, like <see cref="IsMany"/>
    /// 
    /// or
    /// 
    /// 3) (not implemented as of Feb 2017) All kind of classes. TODO: This class itself for instance.
    /// 
    /// TODO: For <see cref="AgoRapideAttribute"/> we could use a boolean-enum of None, True and False. 
    /// TODO: for bool attributes since bool? is not allowed. 
    /// TODO: THIS COULD SOLVE THE DILEMMA OF OVERWRITING FALSE WITH TRUE IN <see cref="AgoRapideAttribute.EnrichFrom"/>
    /// TODO: This idea could also be related to the idea of making an immutable copy of <see cref="AgoRapideAttribute"/>
    /// TODO: (this copy could have user-friendly bool-values instead of an enum)
    /// TODO: (the whole change could be without breaking code at the client side)
    /// </summary>
    public class AgoRapideAttribute : Attribute {

        /// <summary>
        /// Set by <see cref="GetNewDefaultInstance"/>. 
        /// 
        /// See also <see cref="IsInherited"/>. 
        /// </summary>
        public bool IsDefault { get; private set; }

        /// <summary>
        /// The actual property (actual enum-value) which we are an attribute for.
        /// 
        /// Only relevant when attribute for an enum-value. 
        /// TODO: SPLIT <see cref="AgoRapideAttribute"/> into EnumAttribute and ClassAttribute.
        /// 
        /// Not to be set "manually". Set by <see cref="GetAgoRapideAttribute"/>
        /// Will always be set if originates from <see cref="Extensions.GetAgoRapideAttribute{T}"/>
        /// 
        /// Normally you would use the strongly typed <see cref="AgoRapideAttributeT{TProperty}.P"/> instead of <see cref="AgoRapideAttribute.Property"/>
        /// Note that <see cref="Property"/> may be of type <see cref="AgoRapide.CoreProperty"/> for silently mapped values. 
        /// </summary>
        public object Property { get; private set; }

        /// <summary>
        /// The actual class that we are an attribute for. 
        /// 
        /// Only relevant when attribute for a class (type) or enum-"class".
        /// TODO: SPLIT <see cref="AgoRapideAttribute"/> into EnumAttribute and ClassAttribute.
        /// </summary>
        public Type Class { get; private set; }

        ///// <summary>
        ///// Property is read only so you do not inadverdently set it through [PropertyAttribute(Property = "...")] 
        ///// </summary>
        ///// <param name="_enum"></param>
        //public void SetProperty(object _enum) => Property = _enum;

        public void AssertProperty() {
            if (Property == null) throw new NullReferenceException(nameof(Property) + " for " + ToString());
        }

        public CoreProperty CoreProperty { get; set; }

        /// <summary>
        ///  
        /// 
        /// </summary>
        [AgoRapide(
            Description =
                "Used for general sorting. " +
                "A lower value (think like 1'st priority, 2'nd priority) will put object higher up in a typical AgoRapide sorted list",
            LongDescription =
                "Recommended values are:\r\n" +
                "-1 for very important,\r\n" +
                "0 (default) for 'ordinary' and\r\n" +
                "1 for not important.\r\n" +
                "In this manner it will be relatively easy to emphasize or deemphasize single properties without having to give values for all the other properties.\r\n" +
                "Eventually expand to -2, -3 or 2, 3 as needed. ")]
        public int PriorityOrder { get; set; }

        /// <summary>
        /// TODO: Implement automatic creation of uniqueness index in database in Startup.cs
        /// </summary>
        [AgoRapide(
            Description =
                "TRUE means that only one unique (based on case insensitive comparision) value is expected to exist in the database. " +
                "Use this attribute for user account names for instance (like email addresses)",
            LongDescription =
                "See -" + nameof(IDatabase<CoreProperty>.TryAssertUniqueness) + "-. " +
                "A corresponding uniqueness index could also be created in the database")] // TODO: FUTURE DEVELOPEMENT, create such 
        public bool IsUniqueInDatabase { get; set; }
        public void AssertIsUniqueInDatabase() {
            if (!IsUniqueInDatabase) throw new UniquenessException("!" + nameof(IsUniqueInDatabase) + ". Details: " + ToString());
        }

        /// <summary>
        /// Note that you may set Description through either [Description("...")] or [PropertyAttribute(Description = "...")]. The last one takes precedence. 
        /// 
        /// Note: If <see cref="Type"/> is one of your own classes / enums, or one of the AgoRapide classes / enums 
        /// then you are recommended to not set <see cref="Description"/> / <see cref="LongDescription"/> for the enum value  
        /// but instead rely on using <see cref="AgoRapideAttribute"/> belonging to the enum / class given by <see cref="Type"/>
        /// For an example see how it is implemented for <see cref="CoreProperty.CoreMethod"/> and <see cref="AgoRapide.CoreMethod"/>
        /// 
        /// See also <see cref="CoreProperty.Description"/>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Note: If <see cref="Type"/> is one of your own classes / enums, or one of the AgoRapide classes / enums 
        /// then you are recommended to not set <see cref="Description"/> / <see cref="LongDescription"/> for the enum value  
        /// but instead rely on using <see cref="AgoRapideAttribute"/> belonging to the enum / class given by <see cref="Type"/>
        /// For an example see how it is implemented for <see cref="CoreProperty.CoreMethod"/> and <see cref="AgoRapide.CoreMethod"/>
        /// 
        /// See also <see cref="CoreProperty.LongDescription"/>
        /// </summary>
        public string LongDescription { get; set; }

        /// <summary>
        /// Hint about not to expose actual value of Property as JSON / HTML, and to generate
        /// corresponding "password" input fields in HTML.
        /// </summary>
        public bool IsPassword { get; set; }

        /// <summary>
        /// TRUE if property has to exist for <see cref="Parents"/>. Used for <see cref="APIMethodOrigin.Autogenerated"/> <see cref="APIMethod{TProperty}"/> 
        /// in order to construct necessary parameters.
        /// </summary>
        public bool IsObligatory { get; set; }

        /// <summary>
        /// Hint that reading of this property from database shall be done in a strict manner
        /// (value has to exist, and it has to be asserted against Validator)
        /// 
        /// TODO: Should this be adjusted to IsNotStrict instead? Allowing blank values?
        /// 
        /// TODO: Could this be used (in combination with IsNotStrict) in order to allow blank values? 
        /// </summary>
        public bool IsStrict { get; set; } // TODO: Should this be adjusted to IsNotStrict instead?

        /// <summary>
        /// Only relevant when <see cref="Type"/> is <see cref="DateTime"/>
        /// </summary>
        public DateTimeFormat DateTimeFormat { get; set; }

        /// <summary>
        /// See <see cref="CoreProperty.AccessLevelRead"/> 
        /// Note strict default of <see cref="AccessLevel.System"/> 
        /// </summary>        
        public AccessLevel AccessLevelRead { get; set; } = AccessLevel.System;
        /// <summary>
        /// See <see cref="CoreProperty.AccessLevelWrite"/> 
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

        /// <summary>
        /// Signifies properties that there may be several of active at a given time (multiple current) for 
        /// a given <see cref="BaseEntityT{TProperty}"/>.
        /// 
        /// Stored in memory in the <see cref="BaseEntityT{TProperty}.Properties"/>-collection under
        /// one parent whose own <see cref="BaseEntityT{TProperty}.Properties"/>-collection again 
        /// contains each instance with collection index corresponding to <see cref="int.MaxValue"/> minus index.
        /// 
        /// In the database they are stored with index #1, #2 and so on 
        /// appended to actual TProperty enum value 
        /// for instance like phone_number#1, phone_number#2 and so on.
        /// </summary>
        public bool IsMany { get; set; }

        public void AssertIsMany() {
            if (!true.Equals(IsMany)) throw new IsManyException(ToString());
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
        public Type Group { get; set; }

        /// <summary>
        /// The type of this Property. 
        /// 
        /// Typical examples are 
        /// 1) typeof(string), typeof(long), typeof(DateTime), 
        /// 2) typeof(CoreMethod) / typeof(AnyEnum) (Reverse mapping of such enum types are done through <see cref="Util.MapTToTProperty{T, TProperty}"/>). 
        /// 3) Can also be a type assignable to <see cref="ITypeDescriber"/>. 
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Describes entities for which this property is used.
        /// 
        /// Typical example for a {TProperty} enum like P would be:
        ///   [AgoRapide(Parent = typeof(Person), Type = typeof(DateTime))]
        ///   date_of_birth
        ///   
        /// For AgoRapide TProperty-entities (that is, where TProperty is unknown) the generic argument {CoreProperty} is used instead like
        ///   [AgoRapide(Parent = typeof(APIMethod{CoreProperty}), Type = typeof(Environment))]
        ///   Environment
        ///   
        /// Note that once <see cref="Parents"/> have been set for a given property then you will have to also add
        /// <see cref="AccessLevelRead"/> (and <see cref="AccessLevelWrite"/>) 
        /// both for the property AND (IMPORTANT!) ALSO for the entity class itself because 
        /// <see cref="BaseEntityT{TProperty}.GetExistingProperties"/> (through <see cref="Extensions.GetChildPropertiesForAccessLevel"/>) 
        /// will take into account these access rights (which default to <see cref="AccessLevel.System"/>. 
        /// </summary>
        public Type[] Parents { get; set; }

        //private Type _parent = null;
        ///// <summary>
        ///// The Parent entity type for which the Property is used (for which the Property is a member property)
        ///// Note how throws NullReferenceException if not set.
        ///// Could have been called Entity
        ///// </summary>
        //public Type Parent {
        //    get {
        //        if (_parent == null) throw new NullReferenceException(
        //             nameof(_parent) + " == null for " + ToString() + ". " +
        //            "Probable cause: For some P you miss a [Attributes(_parent = typeof(xxx))]) statement. " +
        //            "Note that this exception should never occur because all enums should have been checked at application startup (CheckEnum). " +
        //            "You should ensure that that method really run first when application is started.");
        //        return _parent;
        //    }
        //    set => _parent = value;
        //}
        //public bool ParentIsNotSet => _parent == null;

        /// <summary>
        /// TODO: Implement so that may also be given for Type string (string will be converted to int, and checked for value. Useful for postal codes)
        /// 
        /// TODO: Not implemented in <see cref="AgoRapideAttributeT{TProperty}.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public double MinValueDbl { get; set; }
        /// <summary>
        /// TODO: Implement so that may also be given for Type string (string will be converted to int, and checked for value. Useful for postal codes)
        /// 
        /// TODO: Not implemented in <see cref="AgoRapideAttributeT{TProperty}.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public double MaxValueDbl { get; set; }

        /// <summary>
        /// TODO: Not implemented in <see cref="AgoRapideAttributeT{TProperty}.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public DateTime MinValueDtm { get; set; }
        /// <summary>
        /// TODO: Not implemented in <see cref="AgoRapideAttributeT{TProperty}.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public DateTime MaxValueDtm { get; set; }

        /// <summary>
        /// Only relevant for Type string
        /// TODO: Not implemented in <see cref="AgoRapideAttributeT{TProperty}.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public long MinLength { get; set; }
        /// <summary>
        /// Only relevant for Type string
        /// TODO: Not implemented in <see cref="AgoRapideAttributeT{TProperty}.ValidatorAndParser"/> as of March 2017
        /// </summary>
        public long MaxLength { get; set; }

        /// <summary>
        /// TODO: Not implemented in <see cref="AgoRapideAttributeT{TProperty}.ValidatorAndParser"/> as of March 2017
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
        /// Note that will be set automatically through <see cref="AgoRapideAttributeT{TProperty}(AgoRapideAttribute)"/> for 
        /// <see cref="AgoRapideAttribute"/> having <see cref="AgoRapideAttribute.Type"/> which is <see cref="Type.IsEnum"/> 
        /// 
        /// TODO: Add to <see cref="AgoRapideAttribute.ValidValues"/> a List of tuples with description for each value
        /// TODO: (needed for HTML SELECT tags)
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
        /// Only relevant when attribute for a class. 
        /// TODO: SPLIT <see cref="AgoRapideAttribute"/> into EnumAttribute and ClassAttribute.
        /// 
        /// Indicates for which class this attribute was defined. 
        /// Needed in order to deduce <see cref="IsInherited"/>. 
        /// 
        /// Value must be equivalent to <see cref="Extensions.ToStringVeryShort(Type)"/>
        /// 
        /// TODO: Turn into Type (will require more work for deducing <see cref="IsInherited"/>)
        /// </summary>
        public string DefinedForClass { get; set; }

        /// <summary>
        /// Only relevant when attribute for a class. 
        /// TODO: SPLIT <see cref="AgoRapideAttribute"/> into EnumAttribute and ClassAttribute.
        /// 
        /// Indicates if attribute was defined for a super class and not the actual class. 
        /// 
        /// Set by <see cref="GetAgoRapideAttribute(Type)"/>
        /// 
        /// Depends on <see cref="DefinedForClass"/> being set for super class. 
        /// </summary>
        public bool IsInherited { get; private set; }

        public class NoAttributesDefinedException : ApplicationException {
            public NoAttributesDefinedException(string message) : base(message) { }
            public NoAttributesDefinedException(string message, Exception inner) : base(message, inner) { }
        }

        public override string ToString() => ("Enum: " + Property == null ? "[NULL]" : Property.ToString()) + "\r\nDescription:\r\n" + Description + "\r\nLongDescription:\r\n" + LongDescription;

        // public DData.Unit Unit { get; set; }

        /// <summary>
        /// Typically used by for instance <see cref= "GetAgoRapideAttribute" /> when no attribute found for property.
        /// </summary>
        /// <returns></returns>
        public static AgoRapideAttribute GetNewDefaultInstance() => new AgoRapideAttribute { IsDefault = true };

        /// <summary>
        /// Returns <see cref="AgoRapideAttribute"/> for a class (or enum-"class") itself. 
        /// 
        /// Some of the properties for <see cref="AgoRapideAttribute"/> are not relevant in this case, like <see cref="IsMany"/>. 
        /// Note (for "ordinary" classes) how you will inherit attributes for the base class if no attribute defined for <paramref name="type"/>. 
        /// (<see cref="IsInherited"/> will indicate this assumed that <see cref="DefinedForClass"/> has been set correctly)
        /// 
        /// Usually called from <see cref="Extensions.GetAgoRapideAttribute(Type)"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static AgoRapideAttribute GetAgoRapideAttribute(Type type) {
            var retval = (AgoRapideAttribute)GetCustomAttribute(type, typeof(AgoRapideAttribute));
            if (retval == null) return GetNewDefaultInstance();
            if (type.ToStringVeryShort().Equals("Person")) {
                var a = 1;
            }
            if (string.IsNullOrEmpty(retval.DefinedForClass) || type.ToStringVeryShort().Equals(retval.DefinedForClass)) {
                return retval;
            }

            /// Create whole new instance and set <see cref="IsInherited"/> for it. 
            var newRetval = GetNewDefaultInstance();
            newRetval.EnrichFrom(retval); /// TODO: Ensure that <see cref="EnrichFrom"/> is up-to-date
            newRetval.IsDefault = false;
            newRetval.IsInherited = true;
            return newRetval;
        }

        /// <summary>
        /// Returns <see cref="AgoRapideAttribute"/> for given <paramref name="_enum"/>-value.
        /// 
        /// Note that separate instances with <see cref="IsDefault"/> = True will be created 
        /// for every enum for which no <see cref="AgoRapideAttribute"/> is defined.             
        /// 
        /// Usually called from <see cref="Extensions.GetAgoRapideAttribute{T}(T)"/> / <see cref="Extensions.GetAgoRapideAttribute(object)"/>. 
        /// </summary>
        /// <param name="_enum"></param>
        /// <param name="corePropertyAttributeGetter">
        /// Indicates corresponding <see cref="CoreProperty"/>-attribute to use if <paramref name="_enum"/> is not defined. 
        /// May return null, but in that case an exception will be thrown if <paramref name="_enum"/> is not defined
        /// </param>
        /// <returns></returns>
        public static AgoRapideAttribute GetAgoRapideAttribute(object _enum, Func<AgoRapideAttribute> corePropertyAttributeGetter) {
            // TODO: Consider moving more of this code into AgoRapideAttribute-class
            var type = _enum.GetType();
            var field = type.GetField(_enum.ToString());
            if (field == null) {
                if (corePropertyAttributeGetter == null) throw new NullReferenceException(nameof(corePropertyAttributeGetter));
                return corePropertyAttributeGetter() ?? throw new NullReferenceException(nameof(corePropertyAttributeGetter) + "(): Cause: " + type.ToStringShort() + "." + _enum.ToString() + " is most probably not defined. Note that you can not call this method with " + nameof(_enum) + " as a \"silently-mapped\" " + nameof(CoreProperty));
            }
            var retval = new Func<AgoRapideAttribute>(() => {
                var attributes = field.GetCustomAttributes(typeof(AgoRapideAttribute), true);
                switch (attributes.Length) {
                    case 0:
                        return GetNewDefaultInstance();
                    case 1:
                        var r = (AgoRapideAttribute)attributes[0];
                        if (r.IsDefault) throw new AgoRapideAttributeException(nameof(IsDefault) + " is not allowed set \"manually\". Remove use of " + nameof(IsDefault) + " for " + type.ToStringVeryShort() + "." + _enum.ToString()); // This exception should never happen anyway because IsDefault is read only
                        return r;
                    default:
                        throw new AgoRapideAttributeException(nameof(attributes) + ".Length > 1 (" + attributes.Length + ") for " + type.ToStringVeryShort() + "." + _enum.ToString());
                }
            })();
            retval.Property = _enum;
            if (string.IsNullOrEmpty(retval.Description)) {  // You may set Description through either [Description("...")] or Description = "..."
                retval.Description = new Func<string>(() => { // So if not found as Description = "...", try as [Description("...")] instead
                    var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), true);
                    switch (attributes.Length) {
                        case 0: return "";
                        case 1: return ((DescriptionAttribute)attributes[0]).Description;
                        default: throw new Exception("attributes.Length > 1. Multiple " + typeof(DescriptionAttribute) + " defined for " + type.ToStringVeryShort() + "." + _enum.ToString()); // Should have been stopped by the compiler
                    }
                })();
            }
            if (_enum is CoreProperty) retval.CoreProperty = (CoreProperty)_enum; // Necessary for silently mapped TProperty attributes as they "get" this attribute (retval)
            return retval;
        }

        /// <summary>
        /// Enriches non-set properties of this attribute class with properties from other attribute class
        /// 
        /// Used in order to transfer attributes from <see cref="CoreProperty"/> to <see cref="TProperty"/>.
        /// 
        /// Note how:
        /// 1) Boolean values are only transferred if they are TRUE at <paramref name="other"/>-end.
        /// 2) Enum values are only transferred if they are not default at this end.
        /// 3) Other value types are typically not transferred
        /// </summary>
        /// <param name="other">
        /// Typically attributes for a <see cref="CoreProperty"/>
        /// </param>
        public void EnrichFrom(AgoRapideAttribute other) {

            // TODO: Ensure that all properties are included here (Jan 2017)
            if (Type == null) Type = other.Type;

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

            /// TODO: For <see cref="AgoRapideAttribute"/> we could use a boolean-enum of None, True and False. 
            /// TODO: for bool attributes since bool? is not allowed. 
            /// TODO: THIS COULD SOLVE THE DILEMMA OF OVERWRITING FALSE WITH TRUE IN <see cref="AgoRapideAttribute.EnrichFrom"/>
            /// TODO: This idea could also be related to the idea of making an immutable copy of <see cref="AgoRapideAttribute"/>
            /// TODO: (this copy could have user-friendly bool-values instead of an enum)
            /// TODO: (the whole change could be without breaking code at the client side)

            // if (other.IsDefault) SetAsDefault(); // Removed 8 Mar 2017

            if (other.IsObligatory) IsObligatory = other.IsObligatory;
            if (other.IsStrict) IsStrict = other.IsStrict;
            if (other.IsUniqueInDatabase) IsUniqueInDatabase = other.IsUniqueInDatabase;
            if (other.IsPassword) IsPassword = other.IsPassword;
            if (other.CanHaveChildren) CanHaveChildren = other.CanHaveChildren;
            if (other.IsMany) IsMany = other.IsMany;

            if (DateTimeFormat == DateTimeFormat.None) DateTimeFormat = other.DateTimeFormat;
        }
    }

    public class AgoRapideAttributeException : ApplicationException {
        public AgoRapideAttributeException(string message) : base(message) { }
        public AgoRapideAttributeException(string message, Exception inner) : base(message, inner) { }
    }
}
