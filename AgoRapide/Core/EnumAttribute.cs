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
        /// Do not confuse with <see cref="AgoRapideEnumType"/>
        /// </summary>
        [ClassMember(Description = "The actual type of enum that we are an attribute for")]        
        public Type EnumType => _enumType ?? throw new NullReferenceException(nameof(_enumType) + ". Should have been set by " + nameof(GetAttribute));

        public EnumType AgoRapideEnumType { get; set; }

        public static EnumAttribute GetAttribute(Type type) {
            NotOfTypeEnumException.AssertEnum(type);
            var retval = GetAttributeThroughType<EnumAttribute>(type);
            retval._enumType = type;
            return retval;
        }

        public override string ToString() => nameof(EnumType) + ": " + (_enumType?.ToString() ?? "[NULL]") + ", " + nameof(AgoRapideEnumType) + ": " + AgoRapideEnumType + "\r\n" + base.ToString();
        protected override string GetIdentifier() => GetType().ToStringShort().Replace("Attribute", "") + "_" + EnumType.ToStringShort();
    }
}
