// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// TODO: Ensure correct terminology here (from a math viewpoint)
    /// </summary>
    [Enum(
        Description = "Describes operations on sets of -" + nameof(BaseEntity) + "-. Part of -" + nameof(Context) + "-.",
        AgoRapideEnumType = EnumType.EnumValue
    )]
    public enum SetOperator {
        None,
        Union,
        Intersect,
        Remove
    }
}
