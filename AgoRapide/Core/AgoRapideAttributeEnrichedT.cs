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
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AgoRapideAttributeEnrichedT<T> : AgoRapideAttributeEnriched where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"

        /// <summary>
        /// The actual enum that this class is an attribute for.
        /// 
        /// TODO: UPDATE THIS COMMENT!
        /// 
        /// Corresponds normally to <see cref="AgoRapideAttribute.Property"/> (except when <see cref="P"/> is a silently mapped <see cref="CoreProperty"/>) but more strongly typed.
        /// 
        /// Normally you would use the strongly typed <see cref="AgoRapideAttributeEnrichedT.P"/> instead of <see cref="AgoRapideAttribute.Property"/>
        /// </summary>
        public T P;

        /// <summary>
        /// TODO: REMOVE CODE FROM HERE, put into <see cref="AgoRapideAttributeEnriched.Initialize"/>
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        /// <param name="coreProperty">Relevant when <paramref name="agoRapideAttribute"/> is being mapped to a <see cref="CoreProperty"/></param>
        public AgoRapideAttributeEnrichedT(AgoRapideAttribute agoRapideAttribute, CoreProperty? coreProperty) {
            A = agoRapideAttribute;

            /// TODO: This name is misleading. We could as well be called from <see cref="EnumMapper.MapEnum{T}"/>
            var isAttributeForCorePropertyItself = typeof(T).Equals(typeof(CoreProperty));
            if (coreProperty != null) {
                _coreProperty = coreProperty; /// This looks like an entity property enum like P. Most probably we are called from <see cref="EnumMapper.MapEnum{T}"/>. 
            } else if (isAttributeForCorePropertyItself) {
                _coreProperty = A.Property as CoreProperty? ?? throw new InvalidObjectTypeException(A.Property, typeof(CoreProperty), nameof(A.Property) + ".\r\nDetails: " + ToString());
            } else {
                _coreProperty = null; // This is quite normal. The actual enum is not an entity property enum. 
            }
            if (coreProperty != null && isAttributeForCorePropertyItself) {
                if (!(coreProperty is T)) throw new InvalidObjectTypeException(coreProperty, typeof(T), nameof(coreProperty) + ".\r\nDetails: " + ToString());
                P = (T)(object)coreProperty;
            } else {
                if (!(A.Property is T)) throw new InvalidObjectTypeException(A.Property, typeof(T), nameof(A.Property) + ".\r\nDetails: " + ToString());
                P = (T)A.Property;
            }

            PToString = typeof(T).Equals(typeof(CoreProperty)) ? A.Property.ToString() : P.ToString();
            PExplained = typeof(T).Equals(A.Property.GetType()) ?
                (typeof(T).ToStringVeryShort() + "." + P.ToString()) :
                (A.Property.GetType() + "." + CoreProperty + " (mapped to " + typeof(T).ToStringVeryShort() + "." + P.ToString() + ")");

            Initialize(isAttributeForCorePropertyItself);
        }
    }
}

