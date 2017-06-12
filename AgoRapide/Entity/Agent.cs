using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {
    [Class(
        Description = "See -" + nameof(EntityTypeCategory.Agent) + "-.",
        DefinedForClass = nameof(Agent),
        CacheUse = CacheUse.All // Since there is assumed to be a limited number of elements. 
    )]
    public class Agent : BaseEntity {
    }
}
