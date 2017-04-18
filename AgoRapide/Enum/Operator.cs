﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// TODO: Support IS (like IS NULL)
    /// </summary>
    [AgoRapide(Description = 
        "Operators as used by -" + nameof(QueryId) + "-. " +
        "See also -" + nameof(Extensions.ToSQLString) + "-.")]
    public enum Operator {
        None,
        IN,
        EQ,
        GT,
        LT,
        GEQ,
        LEQ,
        [AgoRapide(Description = "Case sensitive string wildcard comparision as implemented by PostgreSQL database engine.")]
        LIKE,
        [AgoRapide(Description = "Case insensitive string wildcard comparision as implemented by PostgreSQL database engine.")]
        ILIKE
    }
}
