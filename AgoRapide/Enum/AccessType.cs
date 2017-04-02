using System;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {

    [AgoRapide(EnumType = EnumType.DataEnum)]
    public enum AccessType {

        None,

        [AgoRapide(Description = "Equivalent to -" + nameof(CoreP.AccessLevelUse) + "- and -" + nameof(CoreP.AccessLevelRead) + "-")]
        Read,

        [AgoRapide(Description = "Equivalent to -" + nameof(CoreP.AccessLevelWrite) + "-")]
        Write
    }
}
