using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapideSample {

    [Enum(AgoRapideEnumType = EnumType.EnumValue)]
    public enum Colour {
        None,

        [EnumValue(Description = "Bjørn's favourite colour")]
        Red,

        Green,

        Blue
    }
}