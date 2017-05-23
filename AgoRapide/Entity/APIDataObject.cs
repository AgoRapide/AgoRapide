// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    [Class(
        Description =
            "Represents a basic data object that your API provides like Person, Order, Product. " +
            "All your data object classes should inherit this class in order to get better -" + nameof(ResponseFormat.HTML) + "- functionality. " +
            "Not used by the AgoRapide library itself",
        DefinedForClass = nameof(APIDataObject)
    )]
    public class APIDataObject : BaseEntity {
    }
}
