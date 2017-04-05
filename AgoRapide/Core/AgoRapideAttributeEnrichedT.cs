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
    /// Apart from that no differences from <see cref="AgoRapideAttributeEnriched"/>.
    /// 
    /// <see cref="AgoRapideAttributeEnrichedT{T}"/> contains attributes (C# code originated). 
    /// <see cref="AgoRapideAttributeDynamic"/> contains attributes dynamically defined by final user (database originated). 
    /// 
    /// This class is assumed to have marginal use.
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
        /// <param name="coreP">
        /// Only given when called from <see cref="EnumMapper.MapEnum{T}"/>. 
        /// Not given when called from <see cref="Extensions.GetAgoRapideAttributeT{T}"/>. 
        /// Signifies that this is an <see cref="AgoRapide.EnumType.EntityPropertyEnum"/> like <see cref="AgoRapideAttributeEnriched.CoreP"/>, P or similar.
        /// 
        /// TODO: Elaborate on this comment:
        /// Note that will use <see cref="CoreP"/> from <see cref="AgoRapideAttribute.InheritAndEnrichFromProperty"/> instead of that is set. 
        /// </param>
        public AgoRapideAttributeEnrichedT(AgoRapideAttribute agoRapideAttribute, CoreP? coreP) {
            A = agoRapideAttribute;
            _coreP = coreP;
            if (!(A.Property is T)) throw new InvalidObjectTypeException(A.Property, typeof(T), nameof(A.Property) + ".\r\nDetails: " + ToString());
            P = (T)A.Property;
            Initialize();
        }
    }
}

