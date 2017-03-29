using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {
    /// <summary>
    /// TODO: MOVE TO FOLDER "Entity"? OR, LET BE WHERE IS, since have entities all over the place now.
    /// 
    /// Slightly refined version of <see cref="MethodAttribute"/> in order to use against database and as API-result itself.
    /// (since inherits <see cref="BaseEntityT"/>
    /// </summary>
    public class MethodAttributeT : ApplicationPart { 
        public MethodAttribute A { get; private set; }

        public MethodAttributeT(MethodAttribute methodAttribute) {
            A = methodAttribute ?? throw new ArgumentNullException(nameof(methodAttribute));
            AddProperty(CoreProperty.CoreMethod.A(), A.CoreMethod);
            // AddProperty(CoreProperty.RouteTemplate), A.RouteTemplate); // Leave this to ApiMethod instead
            AddProperty(CoreProperty.AccessLevelUse.A(), A.AccessLevelUse);
            AddProperty(CoreProperty.Environment.A(), A.Environment);
            AddProperty(CoreProperty.Description.A(), A.Description + ""); // Avoid null values (TODO: Implement mechanism for setting no-longer-current of existing property instead (when this value becomes null))
            AddProperty(CoreProperty.LongDescription.A(), A.LongDescription + ""); // Avoid null values (TODO: Implement mechanism for setting no-longer-current of existing property instead (when this value becomes null))
            AddProperty(CoreProperty.ShowDetailedResult.A(), A.ShowDetailedResult);
        }
    }
}