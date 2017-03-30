using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;

namespace AgoRapide.Core {

    /// <summary>
    /// Contains the actual <typeparamref name="T"/> enum that this class is an attribute for (<see cref="P"/>).
    /// 
    /// Apart from that no differences from <see cref="AgoRapideAttributeEnriched"/>
    /// 
    /// This class is assumed to have marginal use.
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AgoRapideAttributeEnrichedT<T> : AgoRapideAttributeEnriched where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"

        /// <summary>
        /// The actual enum that this class is an attribute for. 
        /// Corresponds to <see cref="AgoRapideAttributeEnriched.PToString"/>
        /// </summary>
        public T P;

        /// <summary>
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        /// <param name="coreProperty">Signifies that this is an entity property enum. See <see cref="AgoRapideAttributeEnriched.CoreProperty"/></param>
        public AgoRapideAttributeEnrichedT(AgoRapideAttribute agoRapideAttribute, CoreProperty? coreProperty) {            
            A = agoRapideAttribute;
            _coreProperty = coreProperty;
            if (!(A.Property is T)) throw new InvalidObjectTypeException(A.Property, typeof(T), nameof(A.Property) + ".\r\nDetails: " + ToString());
            P = (T)A.Property;
            Initialize();
        }
    }
}

