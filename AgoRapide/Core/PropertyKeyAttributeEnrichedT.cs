using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;

namespace AgoRapide.Core {

    /// <summary>
    /// Attribute originating from C# code.
    /// 
    /// TODO: Candidate for removal. Put functionality into <see cref="PropertyKeyNonStrict"/> instead.
    /// 
    /// Contains the actual <typeparamref name="T"/> enum that this class is an attribute for (<see cref="P"/>).
    /// Apart from that no differences from <see cref="PropertyKeyAttributeEnriched"/>.
    /// 
    /// <see cref="PropertyKeyAttributeEnrichedT{T}"/>: Attribute originating from C# code.
    /// <see cref="PropertyKeyAttributeEnrichedDyn"/>: Attribute originating dynamically (from database / API client, not C# code)
    /// 
    /// This class is assumed to have marginal use.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PropertyKeyAttributeEnrichedT<T> : PropertyKeyAttributeEnriched where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"

        /// <summary>
        /// The actual enum that this class is an attribute for. 
        /// Corresponds to <see cref="PropertyKeyAttributeEnriched.PToString"/>
        /// </summary>
        public T P;

        /// <summary>
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        /// <param name="coreP">
        /// Only given when called from <see cref="EnumMapper.MapEnum{T}"/>. 
        /// Not given when called from <see cref="Extensions.GetAgoRapideAttributeT{T}"/>. 
        /// Signifies that this is an <see cref="AgoRapide.EnumType.PropertyKey"/> like <see cref="PropertyKeyAttributeEnriched.CoreP"/>, P or similar.
        /// 
        /// TODO: Elaborate on this comment:
        /// Note that will use <see cref="CoreP"/> from <see cref="PropertyKeyAttribute.InheritAndEnrichFromProperty"/> instead of that is set. 
        /// </param>
        public PropertyKeyAttributeEnrichedT(PropertyKeyAttribute agoRapideAttribute, CoreP? coreP) {
            A = agoRapideAttribute;
            _coreP = coreP;
            if (!(A.Property is T)) throw new InvalidObjectTypeException(A.Property, typeof(T), nameof(A.Property) + ".\r\nDetails: " + ToString());
            P = (T)A.Property;
            Initialize();
        }
    }
}

