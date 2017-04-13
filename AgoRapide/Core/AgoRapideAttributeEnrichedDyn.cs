using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    /// <summary>
    /// Attribute originating dynamically (from database / API client, not C# code)
    /// 
    /// This class has no properties in addition to <see cref="AgoRapideAttributeEnriched"/> but is used
    /// to clarify origin of the attribute. 
    /// 
    /// <see cref="AgoRapideAttributeEnrichedT{T}"/>: Attribute originating from C# code.
    /// <see cref="AgoRapideAttributeEnrichedDyn"/>: Attribute originating dynamically (from database / API client, not C# code)
    /// 
    /// This class is assumed to have marginal use.
    /// </summary>
    public class AgoRapideAttributeEnrichedDyn : AgoRapideAttributeEnriched {
        public AgoRapideAttributeEnrichedDyn(AgoRapideAttribute agoRapideAttribute, CoreP coreP) {
            A = agoRapideAttribute;
            _coreP = coreP;
            if (!(A.Property is string)) throw new InvalidObjectTypeException(A.Property, typeof(string), nameof(A.Property) + ".\r\nDetails: " + ToString());
            Initialize();
        }
    }
}
