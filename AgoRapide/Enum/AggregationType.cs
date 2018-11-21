// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
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

        [EnumValue(Description = "Closely connected to -" + nameof(AggregationType.Count) + "-. Assumed to be that value as percentage of a total count.")]
        Percent,

        Sum,
        Min,
        Max,
        Average,
        Median
    }
}
