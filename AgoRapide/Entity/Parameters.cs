using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;

namespace AgoRapide {

    /// <summary>
    /// TODO: MOVE INTO API-FOLDER!
    /// 
    /// Never stored in database. 
    /// </summary>
    [Class(Description = "Incoming parameters to an API call. Offered through -" + nameof(ValidRequest) + "-")]
    public class Parameters : BaseEntity { 
        public Parameters(Dictionary<CoreP, Property> parameters) => Properties = parameters;        
        public override string Name => "Collection of parameters to API call";
    }
}
