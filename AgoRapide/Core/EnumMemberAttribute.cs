using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    [Enum(Description = "Describes an enum member (enum value) NOT of type -" + nameof(EnumType.EntityPropertyEnum) + "-. Derived class -" + nameof(AgoRapideAttribute) + "- describes -" + nameof(EnumType.EntityPropertyEnum) + "-")]
    public class EnumMemberAttribute : BaseAttribute {

        private object _enumMember;
        [ClassMember(Description = "The actual enum member (enum value) that we are an attribute for")]
        public object EnumMember => _enumMember ?? throw new NullReferenceException(nameof(_enumMember) + ". Should have been set by -" + nameof(GetAttribute) + "-");

        public static EnumMemberAttribute GetAttribute(object _enum) {
            var type = _enum.GetType();
            NotOfTypeEnumException.AssertEnum(type); // TODO: Necessary? Most possibly YES!
            if (type.GetEnumAttribute().EnumTypeY == EnumType.EntityPropertyEnum) throw new InvalidObjectTypeException(_enum, EnumType.EntityPropertyEnum + " not allowed here");
            var field = type.GetField(_enum.ToString()) ?? throw new NullReferenceException(nameof(type.GetField) + "(): Cause: " + type.ToStringShort() + "." + _enum.ToString() + " is most probably not defined.");

            var retval = new Func<EnumMemberAttribute>(() => {
                var attributes = field.GetCustomAttributes(typeof(EnumMemberAttribute), true);
                switch (attributes.Length) {
                    case 0:
                        var tester = new Action<Type>(t => {
                            object found = field.GetCustomAttributes(t, true);
                            if (found != null) throw new IncorrectAttributeTypeUsedException(found, typeof(EnumMemberAttribute), type + "." + _enum);
                        });
                        tester(typeof(ClassAttribute));
                        tester(typeof(ClassMemberAttribute));
                        tester(typeof(EnumAttribute));
                        // tester(typeof(EnumMemberAttribute));
                        tester(typeof(AgoRapideAttribute));

                        return new EnumMemberAttribute { IsDefault = true };
                    case 1:
                        var r = (EnumMemberAttribute)attributes[0];
                        return r;
                    default:
                        throw new AttributeException(nameof(attributes) + ".Length > 1 (" + attributes.Length + ") for " + type + "." + _enum.ToString());
                }
            })();
            retval._enumMember = _enum;
            return retval;
        }
    }
}
