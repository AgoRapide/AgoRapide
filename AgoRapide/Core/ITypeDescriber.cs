// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {
    /// <summary>
    /// TODO: Move to Type-folder. 
    /// 
    /// Dummy interface indicating that implementing class has a static method called
    ///   EnrichKey 
    /// with the same signature as <see cref="IGroupDescriber.EnrichKey"/>
    /// 
    /// Enables adding of validating and parsing information to <see cref="PropertyKeyAttribute"/> 
    /// by specifying a type implementing this interface in <see cref="PropertyKeyAttribute.Type"/>. 
    /// (In other words, the type implementing this interface is by 
    /// this principle able to describe itself to AgoRapide).
    /// 
    /// <see cref="PropertyKeyAttributeEnriched"/>-properties like 
    /// <see cref="PropertyKeyAttributeEnriched.Cleaner"/>,
    /// <see cref="PropertyKeyAttributeEnriched.ValidatorAndParser"/>
    /// can be set ONLY through this "interface" 
    /// 
    /// <see cref="PropertyKeyAttribute"/>-properties like 
    /// <see cref="PropertyKeyAttribute.ValidValues"/>, 
    /// <see cref="PropertyKeyAttribute.SampleValues"/>, 
    /// <see cref="PropertyKeyAttribute.MinLength"/>,
    /// <see cref="PropertyKeyAttribute.MaxLength"/> and so on
    /// can be set EITHER through this "interface" OR directly with the <see cref="System.Attribute"/> mechanism
    /// 
    /// A typical example would be a type called ZipCode (PostalCode) 
    /// TODO: FIND BETTER EXAMPLES! See P in AgoRapideSample for where to put a good example. 
    /// 
    /// Note how this interface is empty. Any class implementing this interface must have a 
    /// STATIC method with the same signature as <see cref="IGroupDescriber.EnrichKey"/>
    /// (this is a practical choice since it avoids AgoRapide having to instantiate the
    /// class for what is essential a static one-off operation done at application initialization)
    /// 
    /// The implementing class should also override <see cref="object.ToString"/> to a value that is understood by 
    /// the <see cref="PropertyKeyAttributeEnriched.ValidatorAndParser"/> used. 
    /// This result of ToString is also what will be used by <see cref="BaseDatabase.CreateProperty"/>
    /// 
    /// <see cref="ITypeDescriber"/> is not to be confused with <see cref="IGroupDescriber"/>. 
    /// The former is specified through <see cref="PropertyKeyAttribute.Type"/> describing an actual class (usually used for a single TProperty), 
    /// while the latter is specified through <see cref="PropertyKeyAttribute.Group"/> describing common attributes for a group of properties. 
    /// (although technically they both do the one and same kind of operations, enriching <see cref="PropertyKeyAttribute"/> 
    /// and its member class <see cref="PropertyKeyAttribute"/> (<see cref="PropertyKeyAttributeEnriched.A"/>)
    /// </summary>
    public interface ITypeDescriber {
        // Implement public static method in your class.
    }
}
