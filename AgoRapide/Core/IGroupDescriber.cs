using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    /// <summary>
    /// Enabling practical grouping of properties together by specifying a type implementing 
    /// this interface in <see cref="AgoRapideAttribute.Group"/>.
    /// 
    /// The implementation should set COMMON attributes for all the enum-properties in the group 
    /// (group meaning all enum-properties having the SAME <see cref="AgoRapideAttribute.Group"/>-type.   
    /// 
    /// Such common attributes would typically be 
    /// <see cref="AgoRapideAttribute.Parents"/>, 
    /// <see cref="AgoRapideAttribute.AccessLevelRead"/>, 
    /// <see cref="AgoRapideAttribute.AccessLevelWrite"/>
    /// A typical example would be a type called PersonCommonPropertiesGroup
    /// 
    /// Note that this is only of practical use, you are not forced to use <see cref="IGroupDescriber"/>. 
    /// Specifying through <see cref="AgoRapideAttribute.Group"/> is just easier than 
    /// repeatedly applying the same attributes for all properties in the given group.
    /// (in other words it reduces the amount of duplicated code)
    /// 
    /// <see cref="ITypeDescriber"/> is not to be confused with <see cref="IGroupDescriber"/>. 
    /// The former is specified through <see cref="AgoRapideAttribute.Type"/> describing an actual class (usually used for a single TProperty), 
    /// while the latter is specified through <see cref="AgoRapideAttribute.Group"/> describing common attributes for a group of properties. 
    /// (although technically they both do the one and same kind of operations, enriching <see cref="AgoRapideAttributeT{TProperty}"/> 
    /// and its member class <see cref="AgoRapideAttribute"/> (<see cref="AgoRapideAttributeT{TProperty}.A"/>)
    /// </summary>
    public interface IGroupDescriber {

        /// <summary>
        /// This method will be called before any initializing is done in <see cref="AgoRapideAttributeT{TProperty}"/>. 
        /// 
        /// The implementation may change both the properties of 
        /// <see cref="AgoRapideAttributeT{TProperty}"/> and the properties of its member class 
        /// <see cref="AgoRapideAttribute"/> (<see cref="AgoRapideAttributeT{TProperty}.A"/>)
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="agoRapideAttribute"></param>
        void EnrichAttribute<TProperty>(AgoRapideAttributeT<TProperty> agoRapideAttribute) where TProperty : struct, IFormattable, IConvertible, IComparable;  // What we really would want is "where T : Enum"
    }
}
