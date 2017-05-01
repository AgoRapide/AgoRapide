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
        [ClassMember(Description= "The actual type that we are a -" + nameof(ClassAttribute) + "- for")]
        public Type ClassType => _classType ?? throw new NullReferenceException(nameof(_classType) + ". Should have been set by " + nameof(GetAttribute));

        /// <summary>
        /// See <see cref="CoreP.AccessLevelRead"/> 
        /// Note strict default of <see cref="AccessLevel.System"/> 
        /// </summary>        
        [ClassMember(Description="Only relevant when attribute for -" + nameof(BaseEntity) + "-")]
        public AccessLevel AccessLevelRead { get; set; } = AccessLevel.System;

        /// <summary>
        /// See <see cref="CoreP.AccessLevelWrite"/> 
        /// Note strict default of <see cref="AccessLevel.System"/> 
        /// </summary>
        [ClassMember(Description = "Only relevant when attribute for -" + nameof(BaseEntity) + "-")]
        public AccessLevel AccessLevelWrite { get; set; } = AccessLevel.System;

        /// <summary>
        /// Only relevant when attribute for a class implementing <see cref="ITypeDescriber"/>
        /// </summary>
        [ClassMember(Description= "Used for unit testing in order to assert failure of validation.")]
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
                Description += (Description.EndsWith(".") ? "" : ".") + "\r\nCore " + nameof(other.Description) + ": " + other.Description;
            }
            if (string.IsNullOrEmpty(LongDescription)) {
                LongDescription = other.LongDescription;
            } else {
                LongDescription += (LongDescription.EndsWith(".") ? "" : ".") + "\r\nCore " + nameof(other.LongDescription) + ": " + other.LongDescription;
            }

            if (AccessLevelRead == AccessLevel.System) AccessLevelRead = other.AccessLevelRead; // Careful with what is default value here
            if (AccessLevelWrite == AccessLevel.System) AccessLevelWrite = other.AccessLevelWrite; // Careful with what is default value here
        }

        public override string ToString() => nameof(ClassType) + ": " + ( _classType?.ToString() ?? "[NULL]") + "\r\n" + base.ToString();
        protected override Id GetId() => new Id (
            idString: GetType().ToStringShort().Replace("Attribute", "") + "_" + ClassType.ToStringVeryShort().Replace("+",""), /// + will show up for local classes like <see cref="BaseAttribute.IncorrectAttributeTypeUsedException"/>
            idFriendly: ClassType.ToStringShort(),
            idDoc: new List<string> { ClassType.ToStringShort() }
        );
    }
}
