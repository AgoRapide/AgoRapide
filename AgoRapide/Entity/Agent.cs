using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {
    [Class(
        Description =
            "Practical term used to designate a more complex -" + nameof(APIDataObject) + "- " +
            "with independent business logic (like for instance logic for communication with outside systems).",
        LongDescription = 
            "Note the pragmatic approach, combining business logic and data objects",
        DefinedForClass = nameof(Agent),
        CacheUse = CacheUse.All // Since there is assumed to be a limited number of elements. 
    )]
    public class Agent : BaseEntity {
    }
}
