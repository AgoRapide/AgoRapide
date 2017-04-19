using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;

namespace AgoRapide {

    /// <summary>
    /// TODO: Move to Core-folder (no need to collect all <see cref="BaseEntity"/> in one place.
    /// </summary>
    [PropertyKey(
        Description = "Represents a class plus a method within that class in your application like \"{className}.{methodName}\"",
        LongDescription =
            "Used as source of -" + nameof(DBField.cid) + "-, -" + nameof(DBField.vid) + "-, and -" + nameof(DBField.iid) + "- when it is " +
            "\"the system itself\" making changes to your database " +
            "(but this should be the exception, usually -" + nameof(Request.CurrentUser) + "-'s -" + nameof(DBField.id) + "- " +
            "is used in order to pin-point which user credentials was used for any given change in the database).",
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.System
    )]
    public class ClassAndMethod : ApplicationPart { 
    }
}
