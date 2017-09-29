// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    /// <summary>
    /// <see cref="AggregationKey"/> or attribute originating dynamically (from database / API client, not C# code)
    /// 
    /// TODO: Candidate for removal. Put functionality into <see cref="PropertyKey"/> instead.
    /// 
    /// This class has no properties in addition to <see cref="PropertyKeyAttributeEnriched"/> but is used
    /// to clarify origin of the attribute. 
    /// 
    /// <see cref="PropertyKeyAttributeEnrichedT{T}"/>: Attribute originating from C# code.
    /// <see cref="PropertyKeyAttributeEnrichedDyn"/>: <see cref="AggregationKey"/> or attribute originating dynamically (from database / API client, not C# code)
    /// 
    /// This class is assumed to have marginal use.
    /// </summary>
    public class PropertyKeyAttributeEnrichedDyn : PropertyKeyAttributeEnriched {
        public PropertyKeyAttributeEnrichedDyn(PropertyKeyAttribute key, CoreP coreP) {
            A = key;
            _coreP = coreP;
            if (!(A.EnumValue is string)) throw new InvalidObjectTypeException(A.EnumValue, typeof(string), nameof(A.EnumValue) + ".\r\nDetails: " + ToString());
            Initialize();
        }
    }
}
