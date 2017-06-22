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
        Description = "See -" + nameof(EntityTypeCategory.APIDataObject) + "-.",
        DefinedForClass = nameof(APIDataObject)
    )]
    public abstract class APIDataObject : BaseEntity {
    }
}
