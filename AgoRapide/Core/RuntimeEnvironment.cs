using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using AgoRapide.Core;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: REMOVE THIS FILE!!!
    /// 
    /// Immutable class representing the runtime environment that the application runs under. 
    /// 
    /// Offers:
    /// 1) URLs for calling the API (used in response messages / error messages and HTML results)
    /// 2) Knowledge about whether the application runs in a production environment or not
    /// 3) Database specific information (see InsertWellKnownIds)
    /// 4) Themes for HTML interface (to distinguish between test and production environments for instance)    
    /// 
    /// A static instance of this class is expected to be available as BUtil.RuntimeEnvironment
    /// </summary>
    public class RuntimeEnvironment {
        //public class RuntimeEnvironment<THardware> where THardware : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
    }
}
