using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapideSample {
    [Class(
        Description = "A car is a personal transportation device.",
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.Anonymous
    )]
    public class Car : APIDataObject {
        public override string IdFriendly => "The " + PV<Colour>(P.Colour.A()) + " car";
    }
}