﻿// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    /// <summary>
    /// Usually accessed through <see cref="Extensions.GetClassAttribute"/>. 
    /// </summary>    
    [Class(Description =
            "Describes a class.\r\n" +
            "(individual class members are described by -" + nameof(ClassMemberAttribute) + "-)")]
    public class ClassAttribute : BaseAttribute {

        private Type _classType;
        [ClassMember(Description = "The actual type that we are a -" + nameof(ClassAttribute) + "- for")]
        public Type ClassType => _classType ?? throw new NullReferenceException(nameof(_classType) + ". Should have been set by " + nameof(GetAttribute));

        /// <summary>
        /// TODO: Implement inheritance of <see cref="ClassAttribute"/>-members
        /// </summary>
        [ClassMember(Description = "Only relevant when attribute for a -" + nameof(ApplicationPart) + "- / -" + nameof(APIDataObject) + "-.")]
        public CacheUse CacheUse;

        /// <summary>
        /// Only relevant when attribute for <see cref="BaseEntity"/> or <see cref="ITypeDescriber"/>. 
        /// TODO: Consider making an EntityAttribute class in addition to <see cref="ClassAttribute"/>
        /// 
        /// TODO: Should <see cref="ClassAttribute.ParentType"/> and <see cref="ClassAttribute.ChildrenType"/> be replaced with <see cref="PropertyKeyAttribute.ForeignKeyOf"/>?
        /// </summary>
        [ClassMember(
            Description = "The type of the parent (if any). See also -" + nameof(CoreP.QueryIdParent) + "-.",
            LongDescription = "Used for instance by " + nameof(BaseEntity.ToHTMLDetailed) + " in order to give useful links"
        )]
        public Type ParentType;

        /// <summary>
        /// TODO: Should <see cref="ClassAttribute.ParentType"/> and <see cref="ClassAttribute.ChildrenType"/> be replaced with <see cref="PropertyKeyAttribute.ForeignKeyOf"/>?
        /// </summary>
        [ClassMember(
            Description =
                "The type of the children (if any), like " +
                "-" + nameof(ClassMember) + "- being children of -" + nameof(Class) + "- or " +
                "-" + nameof(EnumValue) + "- being children of -" + nameof(AgoRapide.Core.Enum) + "-.",
            LongDescription = "Used for instance by " + nameof(BaseEntity.ToHTMLDetailed) + " in order to give useful links"
        )]
        public Type ChildrenType;

        /// <summary>
        /// Only relevant when attribute for <see cref="BaseEntity"/> or <see cref="ITypeDescriber"/>. 
        /// TODO: Consider making an EntityAttribute class in addition to <see cref="ClassAttribute"/>
        /// </summary>        
        [ClassMember(Description =
            "See -" + nameof(CoreP.AccessLevelRead) + "-. " +
            "Note strict default of -" + nameof(AccessLevel.System) + "-.")]
        public AccessLevel AccessLevelRead { get; set; } = AccessLevel.System;

        /// <summary>
        /// Only relevant when attribute for <see cref="BaseEntity"/> or <see cref="ITypeDescriber"/>. 
        /// TODO: Consider making an EntityAttribute class in addition to <see cref="ClassAttribute"/>
        /// </summary>
        [ClassMember(Description =
                    "See -" + nameof(CoreP.AccessLevelWrite) + "-. " +
                    "Note strict default of -" + nameof(AccessLevel.System) + "-.")]
        public AccessLevel AccessLevelWrite { get; set; } = AccessLevel.System;

        /// <summary>
        /// Only relevant when attribute for a class implementing <see cref="ITypeDescriber"/>
        /// </summary>
        [ClassMember(Description = "Used for unit testing in order to assert failure of validation.")]
        public string[] InvalidValues { get; set; }

        /// <summary>
        /// Only relevant when attribute for a class implementing <see cref="ITypeDescriber"/>
        /// </summary>
        [ClassMember(Description = "Used for\r\n" +
            "1) Unit testing in order to assert validation and\r\n" +
            "2) giving sample values for API calls."
        )]
        public string[] SampleValues { get; set; }

        /// <summary>
        /// Indicates for which class this <see cref="ClassAttribute"/> was defined. 
        /// Needed in order to deduce <see cref="IsInherited"/>. 
        /// 
        /// Value must be equivalent to <see cref="Extensions.ToStringVeryShort(Type)"/>
        /// 
        /// TODO: Turn into Type (will require more work for deducing <see cref="IsInherited"/>)
        /// </summary>
        public string DefinedForClass { get; set; }

        /// <summary>
        /// Indicates if <see cref="ClassAttribute"/> was defined for a super class and not the actual class. 
        /// 
        /// Set by <see cref="GetClassAttribute"/>
        /// 
        /// Depends on <see cref="DefinedForClass"/> being set for super class. 
        /// </summary>
        public bool IsInherited { get; private set; }

        public static ClassAttribute GetAttribute(Type type) {
            OfTypeEnumException.AssertNotEnum(type);
            var retval = GetAttributeThroughType<ClassAttribute>(type);
            retval._classType = type;
            if (string.IsNullOrEmpty(retval.DefinedForClass) || type.ToStringVeryShort().Equals(retval.DefinedForClass)) {
                if (type.BaseType != null) { /// Important to inherit, like <see cref="ClassAttribute.CacheUse"/> as defined for <see cref="ApplicationPart"/>. 
                    retval.EnrichFrom(type.BaseType.GetClassAttribute());
                }
                return retval; /// retval is not inherited from super class
            }
            /// Create whole new instance and set <see cref="IsInherited"/> for it. 
            var newRetval = new ClassAttribute { IsDefault = true, _classType = type };
            newRetval.EnrichFrom(retval); /// TODO: Ensure that code in <see cref="EnrichFrom"/> is up-to-date (last checked Feb 2017)
            newRetval.IsDefault = false; // Correct this
            newRetval.IsInherited = true;
            return newRetval;
        }

        public void EnrichFrom(ClassAttribute other) {
            if (string.IsNullOrEmpty(Description)) {
                Description = other.Description;
            } else {
                Description += (Description.EndsWith(".") ? "" : ".") + "\r\nSuper class -" + other.ClassType.ToStringVeryShort() + "- " + nameof(other.Description) + ": " + other.Description;
            }
            if (string.IsNullOrEmpty(LongDescription)) {
                LongDescription = other.LongDescription;
            } else {
                LongDescription += (LongDescription.EndsWith(".") ? "" : ".") + "\r\nSuper class -" + other.ClassType.ToStringVeryShort() + "- " + nameof(other.LongDescription) + ": " + other.LongDescription;
            }

            if (CacheUse == CacheUse.None) CacheUse = other.CacheUse;

            if (AccessLevelRead == AccessLevel.System) AccessLevelRead = other.AccessLevelRead; // Careful with what is default value here
            if (AccessLevelWrite == AccessLevel.System) AccessLevelWrite = other.AccessLevelWrite; // Careful with what is default value here
        }

        public override string ToString() => nameof(ClassType) + ": " + (_classType?.ToString() ?? "[NULL]") + "\r\n" + base.ToString();
        protected override Id GetId() => new Id(
            idString: new QueryIdString(GetType().ToStringShort().Replace("Attribute", "") + "_" + ClassType.ToStringVeryShort().Replace("+", "")), /// + will show up for local classes like <see cref="BaseAttribute.IncorrectAttributeTypeUsedException"/>
            idFriendly: ClassType.ToStringShort(),
            idDoc: new List<string> { ClassType.ToStringShort() }
        );
    }
}