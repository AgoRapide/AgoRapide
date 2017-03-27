using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;

namespace AgoRapide {

    /// <summary>
    /// Never stored in database. 
    /// </summary>
    [AgoRapide(Description = "Incoming parameters to an API call. Offered through -" + nameof(ValidRequest) + "-")]
    public class Parameters : BaseEntityT { 
        public Parameters(Dictionary<CoreProperty, Property> parameters) => Properties = parameters;        
        public override string Name => "Collection of parameters to API call";
    }
}
