using System;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {

    [Enum(EnumTypeY = EnumType.DataEnum)]
    public enum AccessType {

        None,

        [EnumMember(Description = "Equivalent to -" + nameof(CoreP.AccessLevelUse) + "- and -" + nameof(CoreP.AccessLevelRead) + "-")]
        Read,

        [EnumMember(Description = "Equivalent to -" + nameof(CoreP.AccessLevelWrite) + "-")]
        Write
    }
}
