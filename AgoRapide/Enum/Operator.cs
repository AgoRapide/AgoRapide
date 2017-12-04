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
            "See also -" + nameof(OperatorExtension.ToSQLString) + "-.",
        AgoRapideEnumType = EnumType.EnumValue
    )]
    public enum Operator {
        None,
        IN,
        NEQ, 
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

    public static class OperatorExtension {
        public static string ToMathSymbol(this Operator _operator) {
            switch (_operator) {
                case Operator.EQ: return "=";
                case Operator.NEQ: return "!=";
                case Operator.LT: return "<";
                case Operator.GT: return ">";
                case Operator.LEQ: return "<=";
                case Operator.GEQ: return ">=";
                default: return _operator.ToString(); // TODO: Decide if this approach is good enough
            }
        }
        public static string ToSQLString(this Operator _operator) {
            switch (_operator) {
                case Operator.EQ: return "=";
                case Operator.NEQ: return "<>";
                case Operator.LT: return "<";
                case Operator.GT: return ">";
                case Operator.LEQ: return "<=";
                case Operator.GEQ: return ">=";
                case Operator.LIKE: return "LIKE";
                case Operator.ILIKE: return "ILIKE";
                // TODO: Support IS (like IS NULL)
                default: throw new InvalidEnumException(_operator);
            }
        }
        public static Dictionary<Type, HashSet<Operator>> ValidOperatorsForType = new Dictionary<Type, HashSet<Operator>> {
            { typeof(long), new HashSet<Operator> { Operator.EQ, Operator.NEQ, Operator.GT, Operator.LT, Operator.GEQ, Operator.LEQ } },
            { typeof(double), new HashSet<Operator> { Operator.EQ, Operator.NEQ, Operator.GT, Operator.LT, Operator.GEQ, Operator.LEQ } },
            { typeof(DateTime), new HashSet<Operator> { Operator.EQ, Operator.NEQ, Operator.GT, Operator.LT, Operator.GEQ, Operator.LEQ } },
            { typeof(bool), new HashSet<Operator> { Operator.EQ, Operator.NEQ } },
            { typeof(string), new HashSet<Operator> { Operator.EQ, Operator.NEQ, Operator.LIKE, Operator.ILIKE } },
            { typeof(List<long>), new HashSet<Operator> { Operator.IN } },
            { typeof(List<double>), new HashSet<Operator> { Operator.IN } },
            { typeof(List<DateTime>), new HashSet<Operator> { Operator.IN } },
            { typeof(List<bool>), new HashSet<Operator> { Operator.IN} },
            { typeof(List<string>), new HashSet<Operator> { Operator.IN } },
        };
        public static void AssertValidForType(this Operator _operator, Type type, Func<string> detailer) {
            if (!ValidOperatorsForType.TryGetValue(type, out var validOperators)) throw new InvalidEnumException(_operator, "Not valid for " + type + ". " + nameof(type) + " not recognized at all" + detailer.Result(". Details: "));
            if (!validOperators.Contains(_operator)) throw new InvalidEnumException(_operator, "Invalid for " + type + ". Valid operators are " + string.Join(", ", validOperators.Select(o => o.ToString())) + detailer.Result(". Details: "));
        }
    }
}