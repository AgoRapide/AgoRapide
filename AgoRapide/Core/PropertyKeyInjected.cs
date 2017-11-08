using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {
    /// <summary>
    /// See also <see cref="Percentile"/> which is a somewhat similar concept. 
    /// </summary>
    [Class(
            Description =
                "Property key that is to be automatically injected at runtime by -" + nameof(BaseInjector) + "-.",
            LongDescription =
                "Inheriting classes:\r\n" +
                "-" + nameof(PropertyKeyAggregate) + "-\r\n" +
                "-" + nameof(PropertyKeyExpansion) + "-" +
                "-" + nameof(PropertyKeyJoinTo) + "-"
        )]
    public abstract class PropertyKeyInjected : PropertyKey {
        protected PropertyKeyInjected(PropertyKeyAttributeEnriched key) : base(key) { }
    }
}
