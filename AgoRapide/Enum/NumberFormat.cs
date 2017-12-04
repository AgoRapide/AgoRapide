using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    [Enum(AgoRapideEnumType = EnumType.EnumValue)]
    public enum NumberFormat {
        None,

        [EnumValue(Description = "Example: nnnnn. -" + nameof(ConfigurationAttribute.NumberIdFormat) + "- corresponds to -" + nameof(NumberFormat.Id) + "-.")]
        Id,

        [EnumValue(Description = "Example: n,nnn,nnn -" + nameof(ConfigurationAttribute.NumberIntegerFormat) + "- corresponds to -" + nameof(NumberFormat.Integer) + "-.")]
        Integer,

        [EnumValue(Description = "Example: n,nnn,nnn.00 -" + nameof(ConfigurationAttribute.NumberDecimalFormat) + "- corresponds to -" + nameof(NumberFormat.Decimal) + "-.")]
        Decimal
    }
}
