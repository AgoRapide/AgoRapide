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
    /// <typeparam name="TProperty"></typeparam>
    [AgoRapide(Description = "Incoming parameters to an API call. Offered through -" + nameof(ValidRequest<CoreProperty>) + "-")]
    public class Parameters<TProperty> : BaseEntityT<TProperty> where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
        public Parameters(Dictionary<TProperty, Property<TProperty>> parameters) => Properties = parameters;        
        public override string Name => "Collection of parameters to API call";
    }
}
