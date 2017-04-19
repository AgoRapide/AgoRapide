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
        "(enum members (the individual values) are described by -" + nameof(EnumValueAttribute) + "-)")]
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
            NotOfTypeEnumException.AssertEnum(type);
            var retval = GetAttributeThroughType<EnumAttribute>(type);
            retval._enumType = type;
            return retval;
        }

        public override string ToString() => nameof(EnumTypeX) + ": " + (_enumType?.ToString() ?? "[NULL]") + ", " + nameof(EnumTypeY) + ": " + EnumTypeY + "\r\nDescription:\r\n" + Description + "\r\nLongDescription:\r\n" + LongDescription;
    }
}
