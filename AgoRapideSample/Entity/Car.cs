﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapideSample {
    [AgoRapide(
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.Anonymous
    )]
    public class Car : BaseEntityT<P> {        
    }
}