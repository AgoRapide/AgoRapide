using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    /// <summary>
    /// Usually accessed through <see cref="Extensions.GetEnumAttribute"/>. 
    /// </summary>    
    [Class(Description =
        "Describes an enum-\"class\".\r\n" +
        "(enum members (the individual values) are described by -" + nameof(EnumMemberAttribute) + "-)")]
    public class EnumAttribute : BaseAttribute {

        private Type _enumType;
        /// <summary>
        /// TODO: Rename into something better!
        /// </summary>
        [ClassMember(Description = "The actual type of enum that we are an attribute for")]        
        public Type EnumTypeX => _enumType ?? throw new NullReferenceException(nameof(_enumType) + ". Should have been set by " + nameof(GetAttribute));

        /// <summary>
        /// TODO: Rename into something better!
        /// </summary>
        public EnumType EnumTypeY { get; set; }

        public static EnumAttribute GetAttribute(Type type) {
            var retval = (EnumAttribute)GetCustomAttribute(type, typeof(EnumAttribute));
            if (retval == null) {
                /// TODO: Duplicate code!
                var tester = new Action<Type>(t => {
                    var found = GetCustomAttribute(type, t);
                    if (found != null) throw new IncorrectAttributeTypeUsedException(found, typeof(EnumAttribute), type.ToString());
                });
                tester(typeof(ClassAttribute));
                tester(typeof(ClassMemberAttribute));
                // tester(typeof(EnumAttribute));
                tester(typeof(EnumMemberAttribute));
                tester(typeof(AgoRapideAttribute));
                return new EnumAttribute { IsDefault = true };
            }
            retval._enumType = type;
            return retval;
        }

        public override string ToString() => nameof(EnumTypeX) + ": " + (_enumType?.ToString() ?? "[NULL]") + ", " + nameof(EnumTypeY) + ": " + EnumTypeY + "\r\nDescription:\r\n" + Description + "\r\nLongDescription:\r\n" + LongDescription;
    }
}
