// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// TODO: Support IS (like IS NULL)
    /// 
    /// TODO: Rename in ComparisionOperator since we now have <see cref="SetOperator"/>
    /// </summary>
    [Enum(
        Description =
            "Operators as used by -" + nameof(QueryId) + "-. " +
            "See also -" + nameof(Extensions.ToSQLString) + "-.",
        AgoRapideEnumType = EnumType.EnumValue
    )]
    public enum Operator {
        None,
        IN,
        EQ,
        GT,
        LT,
        GEQ,
        LEQ,
        [EnumValue(Description = "Case sensitive string wildcard comparision as implemented by PostgreSQL database engine.")]
        LIKE,
        [EnumValue(Description = "Case insensitive string wildcard comparision as implemented by PostgreSQL database engine.")]
        ILIKE
    }
}
