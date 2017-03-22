using System;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {

    public enum AccessType {

        None,

        [AgoRapide(Description = "Equivalent to -" + nameof(CoreProperty.AccessLevelUse) + "- and -" + nameof(CoreProperty.AccessLevelRead) + "-")]
        Read,

        [AgoRapide(Description = "Equivalent to -" + nameof(CoreProperty.AccessLevelWrite) + "-")]
        Write
    }
}
