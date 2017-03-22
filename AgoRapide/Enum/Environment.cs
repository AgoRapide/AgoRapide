using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// Environment in AgoRapide are used in different conceptual understandings:
    /// 
    /// 1) To characterize a single property. See <see cref="Core.AgoRapideAttribute"/>.
    ///    The current <see cref="Environment"/> has to be equivalent or lower in order for the property to be shown / accepted.
    ///    
    /// 2) To characterize an API method. See <see cref="API.MethodAttribute"/>.
    ///    The current <see cref="Environment"/> has to be equivalent or lower in order for the method to be included in the API routing
    ///    
    /// 3) To characterize an individual instance of an entity. See <see cref="BaseEntityT{TProperty}"/>. 
    ///    In this manner you may switch on functionality for specific customers only in for instance your
    ///    production environment
    ///    
    /// Uses 1), 2) and 3) gives the possibility of using the same code base in development, test and production. 
    /// This again reduces the need for having branches in your source code repository
    /// 
    /// And last, you may use Environment in order:
    /// 4) (the traditional understanding) To characterize the runtime environment the application runs in 
    ///    (see <see cref="AgoRapide.Core.RuntimeEnvironment"></see>-class)
    /// </summary>
    [AgoRapide(Description = "Characterizes the runtime environment that the application runs in.")]
    public enum Environment {
        None,
        Development,
        Test,
        Production
    }
}
