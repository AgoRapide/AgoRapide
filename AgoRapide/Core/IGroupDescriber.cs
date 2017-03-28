using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: <see cref="IGroupDescriber"/> could be replaced by enum-"class" level attributes now that we (Mar 2017) map
    /// TODO: TO <see cref="CoreProperty"/> instead of FROM <see cref="CoreProperty"/> and can have multiple enums in each project.
    /// TODO: (the enums again can be placed inside each entity class that we want to use)
    /// 
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
    /// (although technically they both do the one and same kind of operations, enriching <see cref="AgoRapideAttributeT"/> 
    /// and its member class <see cref="AgoRapideAttribute"/> (<see cref="AgoRapideAttributeT.A"/>)
    /// </summary>
    public interface IGroupDescriber {

        /// <summary>
        /// This method will be called before any initializing is done in <see cref="AgoRapideAttributeT"/>. 
        /// 
        /// The implementation may change both the properties of 
        /// <see cref="AgoRapideAttributeT{CoreProperty}"/> and the properties of its member class 
        /// <see cref="AgoRapideAttribute"/> (<see cref="AgoRapideAttributeT{CoreProperty}.A"/>)
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        void EnrichAttribute(AgoRapideAttributeT<CoreProperty> agoRapideAttribute);
    }
}
