// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    [Enum(
        Description =
            "HTTP methods like GET or POST. " +
            "The AgoRapide principle is to use -" + nameof(GET) + "- for \"everything\" including create, read, update and delete " +
            "except when the data is incompatible for fitting within an URL in which case -" + nameof(POST) + "- may be used.",
        AgoRapideEnumType = EnumType.EnumValue
    )]
    public enum HTTPMethod {

        None, 

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
