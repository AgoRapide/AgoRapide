using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    [Enum(Description =
        "Describes an enum value NOT of type -" + nameof(EnumType.PropertyKey) + "-. " +
        "Member of -" + nameof(EnumValue) + "-. " +
        "Derived class -" + nameof(PropertyKeyAttribute) + "- describes -" + nameof(EnumType.PropertyKey) + "-")]
    public class EnumValueAttribute : BaseAttribute {

        protected object _enumValue;
        [ClassMember(
            Description = "The actual enum member (enum value) that we are an attribute for",
            LongDescription =
                "Note that for dynamically originated attributes (see -" + nameof(PropertyKeyAttributeEnrichedDyn) + "- " +
                "this value will actually be a string, not an -" + nameof(Enum) + "- (only relevant when sub class -" + nameof(PropertyKeyAttribute) + "-)."
        )]
        public object EnumValue => _enumValue ?? throw new NullReferenceException(nameof(EnumValue) + ". Should have been set by -" + nameof(GetAttribute) + "- or similar.\r\nDetails: " + ToString());

        protected string _EnumValueExplained;
        /// <summary>
        /// Recommended value to use in debugging / exception messages.
        /// 
        /// Equivalent to <see cref="EnumValue"/>.GetType().ToString() + "." + <see cref="_enumValue"/>.ToString() for <see cref="EnumValueAttribute"/>
        /// 
        /// For sub class <see cref="PropertyKeyAttribute"/> explains in more detail how this originates. 
        /// 
        /// Typical examples:
        /// CoreP.Username
        /// P.Email &lt;- CoreP.Username (when <see cref="PropertyKeyAttribute.InheritAndEnrichFromProperty"/> is used)
        /// P.FirstName (CoreP 42) (when no corresponding <see cref="CoreP"/> exists. 
        /// 
        /// TODO: If very high value (like almost MaxInt), then explain this as a IsMany-property where P is the index
        /// </summary>
        public string EnumValueExplained => _EnumValueExplained ?? throw new NullReferenceException(nameof(_EnumValueExplained) + ". Should have been set by -" + nameof(GetAttribute) + "-.\r\nDetails: " + ToString());
        public void SetEnumValueExplained(string enumValueExplained) => _EnumValueExplained = enumValueExplained;

        public static EnumValueAttribute GetAttribute(object _enum) {
            var type = _enum.GetType();
            NotOfTypeEnumException.AssertEnum(type, () => nameof(_enum) + ": " + _enum.ToString());
            if (type.GetEnumAttribute().AgoRapideEnumType == EnumType.PropertyKey) {
                // throw new InvalidObjectTypeException(_enum, EnumType.PropertyKey + " not allowed here");
                return PropertyKeyAttribute.GetAttribute(_enum); // Quite OK since this is a sub class
            }
            var field = type.GetField(_enum.ToString()) ?? throw new NullReferenceException(nameof(type.GetField) + "(): Cause: " + type + "." + _enum + " is most probably not defined.");
            var retval = GetAttributeThroughFieldInfo<EnumValueAttribute>(field, () => type + "." + _enum);
            retval._enumValue = _enum;
            retval._EnumValueExplained = retval.EnumValue.GetType() + "." + _enum.ToString();
            return retval;
        }

        public override string ToString() => nameof(EnumValue) + ": " + (_enumValue?.ToString() ?? "[NULL]") + "\r\n" + base.ToString();
        protected override Id GetId() => new Id(
            idString:
                GetType().ToStringShort().Replace("Attribute", "") + "_" +
                EnumValue.GetType().ToStringShort().Replace("+", "") + "_" +/// + is for local classes like <see cref="ConfigurationAttribute.ConfigurationKey"/> 
                EnumValue.ToString(),
            idFriendly: EnumValue.GetType().ToStringShort() + "." + EnumValue.ToString(),
            idDoc: new List<string> {
                EnumValue.GetType().ToStringShort() + "." + EnumValue.ToString(),
                EnumValue.ToString()
            }
        );
    }
}
