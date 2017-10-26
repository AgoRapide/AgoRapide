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
        AgoRapideEnumType = EnumType.EnumValue,
        Description = "Used by -" + nameof(PropertyKeyAggregate) + "- and -" + nameof(AggregationKey) + "-."
    )]
    public enum AggregationType {
        None,
        Count,
        Sum,
        Min,
        Max
    }
}
