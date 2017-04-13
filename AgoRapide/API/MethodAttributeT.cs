using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {
    /// <summary>
    /// TODO: MOVE TO FOLDER "Entity"? OR, LET BE WHERE IS, since have entities all over the place now.
    /// </summary>
    [AgoRapide(Description = 
        "Slightly refined version of -" + nameof(MethodAttribute) + "- in order to use " +
        "against database and " +
        "as API-result itself (since inherits -" + nameof(BaseEntity) + "-)")]
    public class MethodAttributeT : ApplicationPart {
        public MethodAttribute A { get; private set; }

        public MethodAttributeT(MethodAttribute methodAttribute) {
            A = methodAttribute ?? throw new ArgumentNullException(nameof(methodAttribute));

            // Note how we are not adding None-values since they will be considered invalid at later reading from database.
            if (A.CoreMethod != CoreMethod.None) AddProperty(CoreP.CoreMethod.A(), A.CoreMethod);
            if (A.AccessLevelUse != AccessLevel.None) AddProperty(CoreP.AccessLevelUse.A(), A.AccessLevelUse);
            if (A.Environment != Environment.None) AddProperty(CoreP.Environment.A(), A.Environment);

            AddProperty(CoreP.Description.A(), A.Description + ""); // Avoid null values (TODO: Implement mechanism for setting no-longer-current of existing property instead (when this value becomes null))
            AddProperty(CoreP.LongDescription.A(), A.LongDescription + ""); // Avoid null values (TODO: Implement mechanism for setting no-longer-current of existing property instead (when this value becomes null))
            AddProperty(CoreP.ShowDetailedResult.A(), A.ShowDetailedResult);
        }
    }
}