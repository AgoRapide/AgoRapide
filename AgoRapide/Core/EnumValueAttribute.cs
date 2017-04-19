using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    [Enum(Description = "Describes an enum member (enum value) NOT of type -" + nameof(EnumType.PropertyKey) + "-. Derived class -" + nameof(PropertyKeyAttribute) + "- describes -" + nameof(EnumType.PropertyKey) + "-")]
    public class EnumValueAttribute : BaseAttribute {

        private object _enumMember;
        [ClassMember(Description = "The actual enum member (enum value) that we are an attribute for")]
        public object EnumMember => _enumMember ?? throw new NullReferenceException(nameof(_enumMember) + ". Should have been set by -" + nameof(GetAttribute) + "-");

        public static EnumValueAttribute GetAttribute(object _enum) {
            var type = _enum.GetType();
            NotOfTypeEnumException.AssertEnum(type, () => nameof(_enum) + ": " + _enum.ToString()); 
            if (type.GetEnumAttribute().EnumTypeY == EnumType.PropertyKey) throw new InvalidObjectTypeException(_enum, EnumType.PropertyKey + " not allowed here");
            var field = type.GetField(_enum.ToString()) ?? throw new NullReferenceException(nameof(type.GetField) + "(): Cause: " + type.ToStringShort() + "." + _enum.ToString() + " is most probably not defined.");
            var retval = GetAttributeThroughFieldInfo<EnumValueAttribute>(field, () => type + "." + _enum);
            retval._enumMember = _enum;
            return retval;
        }
    }
}
