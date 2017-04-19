using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// The AgoRapide principle is to use <see cref="GET"/> for "everything" including create, read, update and delete 
    /// except when the data is incompatible for fitting within an URL in which case <see cref="POST"/> may be used.
    /// </summary>
    [PropertyKey(EnumType = EnumType.EnumValue)]
    public enum HTTPMethod {

        GET,

        POST,

        /// <summary>
        /// Supported by AgoRapide but suggested not to use
        /// </summary>
        PUT,

        /// <summary>
        /// Supported by AgoRapide but suggested not to use
        /// </summary>
        DELETE
    }
}
