using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapideSample {
    public enum Colour {
        None,

        [AgoRapide(Description = "Bjørn's favourite colour")]
        Red,

        Green,

        Blue
    }
}