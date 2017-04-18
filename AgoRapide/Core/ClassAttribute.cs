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
            var retval = (ClassAttribute)GetCustomAttribute(type, typeof(ClassAttribute));
            if (retval == null) {
                /// TODO: Duplicate code!
                var tester = new Action<Type>(t => {
                    var found = GetCustomAttribute(type, t);
                    if (found != null) throw new IncorrectAttributeTypeUsedException(found, typeof(ClassAttribute), type.ToString());
                });
                // tester(typeof(ClassAttribute));
                tester(typeof(ClassMemberAttribute));
                tester(typeof(EnumAttribute));
                tester(typeof(EnumMemberAttribute));
                tester(typeof(AgoRapideAttribute));

                return GetNewDefaultInstance(type);
            }
            retval._classType = type;
            if (string.IsNullOrEmpty(retval.DefinedForClass) || type.ToStringVeryShort().Equals(retval.DefinedForClass)) {
                return retval;
            }
            /// Create whole new instance and set <see cref="IsInherited"/> for it. 
            var newRetval = GetNewDefaultInstance(type);
            newRetval.EnrichFrom(retval); /// TODO: Ensure that code in <see cref="EnrichFrom"/> is up-to-date (last checked Feb 2017)
            newRetval.IsDefault = false;
            newRetval.IsInherited = true;
            return newRetval;
        }

        public override string ToString() => (_classType == null ? (nameof(ClassType) + ": [NULL]") : (ClassType.IsEnum ? "Enum: " : "Class: "))  + "\r\nDescription:\r\n" + Description + "\r\nLongDescription:\r\n" + LongDescription;

        /// <summary>
        /// Typically used by for instance <see cref= "GetClassAttribute" /> when no attribute found.
        /// </summary>
        /// <returns></returns>
        public static ClassAttribute GetNewDefaultInstance(Type type) => new ClassAttribute { IsDefault = true, _classType = type };

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
    }
}
