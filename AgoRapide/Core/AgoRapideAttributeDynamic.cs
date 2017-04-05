using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    /// <summary>
    /// This class has no properties in addition to <see cref="AgoRapideAttributeEnriched"/> but is used
    /// to clarify origin of the attribute. 
    /// 
    /// <see cref="AgoRapideAttributeEnrichedT{T}"/> contains attributes (C# code originated). 
    /// <see cref="AgoRapideAttributeDynamic"/> contains attributes dynamically defined by final user (database originated). 
    /// 
    /// This class is assumed to have marginal use.
    /// </summary>
    public class AgoRapideAttributeDynamic : AgoRapideAttributeEnriched {
        public AgoRapideAttributeDynamic(AgoRapideAttribute agoRapideAttribute, CoreP coreP) {
            A = agoRapideAttribute;
            _coreP = coreP;
            if (!(A.Property is string)) throw new InvalidObjectTypeException(A.Property, typeof(string), nameof(A.Property) + ".\r\nDetails: " + ToString());
            Initialize();
        }
    }
}
