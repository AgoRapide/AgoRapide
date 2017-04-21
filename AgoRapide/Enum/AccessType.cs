using System;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {

    [Enum(AgoRapideEnumType = EnumType.EnumValue)]
    public enum AccessType {

        None,

        [EnumValue(Description = "Equivalent to -" + nameof(CoreP.AccessLevelUse) + "- and -" + nameof(CoreP.AccessLevelRead) + "-")]
        Read,

        [EnumValue(Description = "Equivalent to -" + nameof(CoreP.AccessLevelWrite) + "-")]
        Write
    }
}
