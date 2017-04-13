using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// Operators as used by <see cref="QueryId"/>
    /// 
    /// TODO: Support IS (like IS NULL)
    /// 
    /// See also <see cref="Extensions.ToSQLString(Operator)"/>
    /// </summary>
    public enum Operator {
        None,
        IN,
        EQ,
        GT,
        LT,
        GEQ,
        LEQ,
        /// <summary>
        /// Case sensitive string wildcard comparision as implemented by PostgreSQL database engine.
        /// </summary>
        LIKE,
        /// <summary>
        /// Case insensitive string wildcard comparision as implemented by PostgreSQL database engine.
        /// </summary>
        ILIKE
    }
}
