using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {
    /// <summary>
    /// Dummy interface indicating that implementing class has a static method 
    /// EnrichAttribute available with the same signature as <see cref="IGroupDescriber.EnrichAttribute"/>
    /// TODO:
    /// TODO: Document how to handle use of {P} as generic type argument. Signature will NOT be the same
    /// TODO:
    /// 
    /// Enables adding of validating and parsing information to <see cref="AgoRapideAttributeT"/> 
    /// by specifying a type implementing this interface in <see cref="AgoRapideAttribute.Type"/>. 
    /// (In other words, the type implementing this interface is by 
    /// this principle able to describe itself to AgoRapide).
    /// 
    /// <see cref="AgoRapideAttributeT"/>-properties like 
    /// <see cref="AgoRapideAttributeEnrichedT.Cleaner"/>,
    /// <see cref="AgoRapideAttributeEnrichedT.ValidatorAndParser"/>
    /// can be set ONLY through this "interface" 
    /// 
    /// <see cref="AgoRapideAttribute"/>-properties like 
    /// <see cref="AgoRapideAttribute.ValidValues"/>, 
    /// <see cref="AgoRapideAttribute.SampleValues"/>, 
    /// <see cref="AgoRapideAttribute.MinLength"/>,
    /// <see cref="AgoRapideAttribute.MaxLength"/> and so on
    /// can be set EITHER through this "interface" OR directly with the <see cref="System.Attribute"/> mechanism
    /// 
    /// A typical example would be a type called ZipCode (PostalCode) 
    /// TODO: FIND BETTER EXAMPLES! See P in AgoRapideSample for where to put a good example. 
    /// 
    /// Note how this interface is empty. Any class implementing this interface must have a 
    /// STATIC method with the same signature as <see cref="IGroupDescriber.EnrichAttribute"/>
    /// (this is a practical choice since it avoids AgoRapide having to instantiate the
    /// class for what is essential a static one-off operation done at application initialization)
    /// 
    /// The implementing class should also override <see cref="object.ToString"/> to a value that is understood by 
    /// the <see cref="AgoRapideAttributeEnrichedT.ValidatorAndParser"/> used. 
    /// This result of ToString is also what will be used by <see cref="IDatabase.CreateProperty"/>
    /// 
    /// <see cref="ITypeDescriber"/> is not to be confused with <see cref="IGroupDescriber"/>. 
    /// The former is specified through <see cref="AgoRapideAttribute.Type"/> describing an actual class (usually used for a single TProperty), 
    /// while the latter is specified through <see cref="AgoRapideAttribute.Group"/> describing common attributes for a group of properties. 
    /// (although technically they both do the one and same kind of operations, enriching <see cref="AgoRapideAttributeT"/> 
    /// and its member class <see cref="AgoRapideAttribute"/> (<see cref="AgoRapideAttributeEnrichedT.A"/>)
    /// </summary>
    public interface ITypeDescriber {
        // Implement public static method in your class.
    }
}
