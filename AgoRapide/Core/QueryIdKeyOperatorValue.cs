// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {
    /// <summary>
    /// TODO: To be extended. Generation of <see cref="QueryId.SQLWhereStatement"/> for instance does not work. 
    /// </summary>
    [Class(
        Description = "The general form of -" + nameof(QueryId) + "-. ",
        SampleValues = new string[] {
            "WHERE " + nameof(EntityTypeCategory) + " = " + nameof(EntityTypeCategory.APIDataObject) /// = corresponds to <see cref="Operator.EQ"/>
        }
    )]
    public class QueryIdKeyOperatorValue : QueryId {

        /// <summary>
        /// TODO: Check initialization of this
        /// </summary>
        public PropertyKeyAttributeEnriched Key { get; private set; }

        public Operator Operator { get; private set; }
        public object Value { get; private set; }

        /// <summary>
        /// TODO: Add a LIMIT parameter to <see cref="QueryIdKeyOperatorValue"/>.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="_operator"></param>
        /// <param name="value">
        /// Note how this may even be a list, in which case code like 
        ///   WHERE value IN (42, 43) 
        /// will be generated.
        /// </param>
        public QueryIdKeyOperatorValue(PropertyKeyAttributeEnriched key, Operator _operator, object value) {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Operator = _operator != Operator.None ? _operator : throw new InvalidEnumException(_operator);
            Value = value;
            if (Value == null) {
                switch (_operator) {
                    case Operator.EQ:
                    case Operator.NEQ:
                        break;
                    default:
                        throw new ArgumentNullException(nameof(value) + ". NULL only allowed for " + nameof(_operator) + " " + Operator.EQ + " or " + Operator.NEQ);
                }
            }

            if (Value == null) {
                SQLQueryNotPossible = true;
                _SQLWhereStatement = null;

                IsSingle = false;
                IsMultiple = true;

                return;
            }

            if (Operator == Operator.EQ && Key.A.IsUniqueInDatabase) {
                IsSingle = true;
                IsMultiple = false;
            } else {
                IsSingle = false;
                IsMultiple = true;
            }

            /// "Normally" we expect there to be only one parameter. 
            /// We have to number parameters for instance for <see cref="Operator.IN"/> and for multiple <see cref="Properties"/>
            var parameterNo = 0;

            var singlePropertySQLConstructor = new Func<PropertyKeyAttributeEnriched, string>(A => {
                var sql = new StringBuilder();
                var detailer = new Func<string>(() => A.PToString + " " + Operator + " " + Value + " (of type " + Value.GetType() + ")");

                if (((int)(object)(A.CoreP)) == 0) throw new InvalidEnumException(A.CoreP, detailer.Result("Details: "));

                sql.Append(DBField.key + " = '" + A.PToString + "' AND ");

                /// Builds SQL query if Value corresponds to T
                T? valueAs<T>(DBField dbField) where T : struct
                {
                    var retval = Value as T?;
                    if (retval != null) {
                        Operator.AssertValidForType(typeof(T), detailer);
                        switch (dbField) {
                            case DBField.lngv:
                            case DBField.blnv:
                                sql.Append(dbField + " " + Operator.ToSQLString() + " " + retval.ToString()); break;
                            default:
                                parameterNo++;
                                sql.Append(dbField + " " + Operator.ToSQLString() + " :" + dbField + (parameterNo).ToString());
                                SQLWhereStatementParameters.Add((dbField + (parameterNo).ToString(), retval));
                                break;
                        }
                    }
                    return retval;
                }

                /// Builds SQL query if Value corresponds to List{T}
                /// (operator will typically be <see cref="Operator.IN"/>)
                List<T> valueAsList<T>(DBField dbField) // TODO: Should this be IEnumerable instead?
                {
                    var retval = Value as List<T>;
                    if (retval != null) {
                        Operator.AssertValidForType(typeof(List<T>), detailer);
                        switch (dbField) {
                            case DBField.lngv:
                            case DBField.blnv:
                                sql.Append(dbField + " " + Operator.ToSQLString() + " (" + string.Join(", ", retval.Select(v => v.ToString())) + ")"); break;
                            default:
                                sql.Append(dbField + " " + Operator.ToSQLString() + " (\r\n");
                                retval.ForEach(v => {
                                    parameterNo++;
                                    sql.Append(":" + dbField + (parameterNo).ToString() + ",\r\n");
                                    SQLWhereStatementParameters.Add((dbField + (parameterNo).ToString(), v));
                                });
                                sql.Remove(sql.Length - 3, 3); // Remove last comma + \r\n
                                sql.Append("\r\n)");
                                break;
                        }
                    }
                    return retval;
                }

                /// Builds SQL query according to Value
                new Action(() => {
                    if (valueAs<long>(DBField.lngv) != null) return;
                    if (valueAs<double>(DBField.dblv) != null) return;
                    if (valueAs<bool>(DBField.blnv) != null) return;
                    if (valueAs<DateTime>(DBField.dtmv) != null) return;
                    {
                        switch (Value) {
                            case string _string:
                                Operator.AssertValidForType(typeof(string), detailer);
                                parameterNo++;
                                sql.Append(DBField.strv + " " + Operator.ToSQLString() + " :" + DBField.strv + (parameterNo).ToString());
                                SQLWhereStatementParameters.Add((DBField.strv + (parameterNo).ToString(), _string));
                                return;
                            case Type type: // Added 7 Jun 2017
                                Operator.AssertValidForType(typeof(string), detailer);
                                parameterNo++;
                                sql.Append(DBField.strv + " " + Operator.ToSQLString() + " :" + DBField.strv + (parameterNo).ToString());
                                SQLWhereStatementParameters.Add((DBField.strv + (parameterNo).ToString(), type.ToStringDB())); /// TODO: Storing of types in database may vary (???). What about <see cref="CoreP.EntityType"/>?
                                return;
                        }
                    }
                    if (Value.GetType().IsEnum) {
                        Operator.AssertValidForType(typeof(string), detailer);
                        sql.Append(DBField.strv + " " + Operator.ToSQLString() + " '" +
                            (PropertyKeyMapper.TryGetA(Value.ToString(), out var k) ? k.Key.PToString : Value.ToString()) +
                            "'");
                        return;
                    }
                    if (Value is ITypeDescriber) {
                        Operator.AssertValidForType(typeof(string), detailer);
                        parameterNo++;
                        sql.Append(DBField.strv + " " + Operator.ToSQLString() + " :" + DBField.strv + (parameterNo.ToString()));
                        SQLWhereStatementParameters.Add((DBField.strv + (parameterNo.ToString()), Value.ToString()));
                        return;
                    }
                    if (valueAsList<long>(DBField.lngv) != null) return;
                    if (valueAsList<double>(DBField.dblv) != null) return;
                    if (valueAsList<bool>(DBField.blnv) != null) return;
                    if (valueAsList<DateTime>(DBField.dtmv) != null) return;
                    if (valueAsList<string>(DBField.strv) != null) return;
                    if (valueAsList<Type>(DBField.strv) != null) return; // Added 7 Jun 2017

                    // TODO: Support for List<SomeEnum> is not yet implemented. 
                    // 
                    // This would be too naive:
                    //   if (valueAsList<CoreP>(DBField.strv) != null) return;
                    // (among other, silently mapped properties would not be supported (The ToString value will be the int-value)).
                    // This just is something totally different (Enum is just an abstract class)
                    //   if (valueAsList<Enum>(DBField.strv) != null) return;
                    // 
                    // We are also without means to recognize other enums used in the application, especially the users
                    // We could cast to IEnumerable<object> though...

                    if (valueAsList<ITypeDescriber>(DBField.strv) != null) return;

                    throw new InvalidObjectTypeException(Value,
                        "Expected " + nameof(Value) + " as long, double, bool, DateTime, string, " + nameof(ITypeDescriber) + " (or list of those), " +
                        "some kind of enum (but list of enums is not currently supported)" + detailer.Result(". Details: "));

                })();
                return sql.ToString();
            });
            _SQLWhereStatement = singlePropertySQLConstructor(key);
        }

        /// <summary>
        /// TODO: Implement for more than <see cref="Operator.EQ"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool IsMatch(BaseEntity entity) {
            if (Key == null) throw new NullReferenceException(nameof(Key) + ". Details: " + ToString());
            if (entity.Properties == null || !entity.Properties.TryGetValue(Key.CoreP, out var p)) {
                if (Value == null) {
                    switch (Operator) {
                        case Operator.NEQ: return false;
                        case Operator.EQ: return true;
                            default: throw new InvalidEnumException(Operator, nameof(Operator) + ": " + Operator + " for NULL");
                    }
                }
                return false;
            }
            if (Value == null) {
                switch (Operator) {
                    case Operator.NEQ: return true;
                    case Operator.EQ: return false;
                    default: throw new InvalidEnumException(Operator, nameof(Operator) + ": " + Operator + " for NULL");
                }
            }
            switch (Value) {
                case Quintile quintile: // TODO: ADD OTHER QUANTILES HERE!
                    if (!p.PercentileIsSet) throw new Property.InvalidPropertyException("!" + nameof(p.PercentileIsSet) + " for " + p.ToString() + ".\r\n" + nameof(entity) + ": " + entity.ToString());
                    switch (Operator) {
                        case Operator.LT: return quintile < p.Percentile.AsQuintile;
                        case Operator.LEQ: return quintile <= p.Percentile.AsQuintile;
                        case Operator.EQ: return quintile == p.Percentile.AsQuintile;
                        case Operator.GEQ: return quintile >= p.Percentile.AsQuintile;
                        case Operator.GT: return quintile > p.Percentile.AsQuintile;
                        case Operator.NEQ: return quintile != p.Percentile.AsQuintile;
                        default: throw new InvalidEnumException(Operator, nameof(Operator) + ": " + Operator + " for " + Value.GetType());
                    }
                case Percentile percentile:
                    if (!p.PercentileIsSet) throw new Property.InvalidPropertyException("!" + nameof(p.PercentileIsSet) + " for " + p.ToString() + ".\r\n" + nameof(entity) + ": " + entity.ToString());
                    switch (Operator) {
                        case Operator.LT: return percentile.Value < p.Percentile.Value;
                        case Operator.LEQ: return percentile.Value <= p.Percentile.Value;
                        case Operator.EQ: return percentile.Value == p.Percentile.Value;
                        case Operator.GEQ: return percentile.Value >= p.Percentile.Value;
                        case Operator.GT: return percentile.Value > p.Percentile.Value;
                        case Operator.NEQ: return percentile.Value != p.Percentile.Value;
                        default: throw new InvalidEnumException(Operator, nameof(Operator) + ": " + Operator + " for " + Value.GetType());
                    }
                case DateTimeComparer dateTimeComparer: {
                        var dateTime = p.V<DateTime>();
                        switch (Operator) {
                            case Operator.EQ: return dateTime >= DateTimeComparerGEQ && dateTime < DateTimeComparerLT;
                            case Operator.NEQ: return dateTime < DateTimeComparerGEQ || dateTime >= DateTimeComparerLT;
                            default: throw new InvalidEnumException(Operator, nameof(Operator) + ": " + Operator + " for " + Value.GetType());
                        }
                    }
                case DateTime dateTime:
                    var entityDateTime = p.V<DateTime>();
                    switch (Operator) {
                        case Operator.LT: return entityDateTime < dateTime;
                        case Operator.LEQ: return entityDateTime <= dateTime;
                        case Operator.EQ: return entityDateTime == dateTime;
                        case Operator.GEQ: return entityDateTime >= dateTime;
                        case Operator.GT: return entityDateTime > dateTime;
                        case Operator.NEQ: return entityDateTime != dateTime;
                        default: throw new InvalidEnumException(Operator, nameof(Operator) + ": " + Operator + " for " + Value.GetType());
                    }
                default:
                    switch (Operator) {
                        case Operator.EQ:
                            if (p.Key.Key.A.IsMany) { 
                                // Return TRUE if any item in list matches.
                                /// Note (very) pragmatic approach here to IsMany. This is practical for drill-down queries for instance (<see cref="API.DrillDownSuggestion.Create"/> for instance)
                                /// TODO: Create a more formalised and mathematically sound approach for this.
                                return (p.Value as List<object>)?.Any(o => o.Equals(Value)) ?? throw new InvalidObjectTypeException(p.Value, typeof(List<object>), ToString());
                            }
                            return p.Value.Equals(Value);
                        case Operator.NEQ:
                            if (p.Key.Key.A.IsMany) {
                                // Return TRUE if no item in list matches. See also comment above.
                                return !(p.Value as List<object>)?.Any(o => o.Equals(Value)) ?? throw new InvalidObjectTypeException(p.Value, typeof(List<object>), ToString());
                            }
                            return !p.Value.Equals(Value);
                        default:
                            var lngValue = Value as long?;
                            if (lngValue != null) {
                                switch (Operator) {
                                    case Operator.LT: return p.V<long>() < lngValue;
                                    case Operator.LEQ: return p.V<long>() <= lngValue;
                                    case Operator.GEQ: return p.V<long>() >= lngValue;
                                    case Operator.GT: return p.V<long>() > lngValue;
                                    default: throw new NotImplementedException(nameof(Operator) + ": " + Operator + " for " + Value.GetType());
                                }
                            } else {
                                throw new NotImplementedException(nameof(Operator) + ": " + Operator + " for " + Value.GetType());
                            }
                    }
            }
        }

        private DateTime? _dateTimeComparerGEQ;
        /// <summary>
        /// Note that once value has been calculated it is cached. The object instance in itself should therefore 
        /// only be used in short-lived scenarios.
        /// </summary>
        private DateTime DateTimeComparerGEQ => _dateTimeComparerGEQ ?? (DateTime)(_dateTimeComparerGEQ = new Func<DateTime>(() => {
            var now = DateTime.Now; var d = Value as DateTimeComparer? ?? throw new InvalidObjectTypeException(Value, typeof(DateTimeComparer));
            switch (d) {
                case DateTimeComparer.HourThis: return new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
                case DateTimeComparer.HourLast: return new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(-1);
                case DateTimeComparer.DayToday: return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                case DateTimeComparer.Day_Yesterday: return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(-1);
                case DateTimeComparer.Day2DaysAgo: return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(-2);
                case DateTimeComparer.MonthThisMonth: return new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                case DateTimeComparer.MonthThisLastYear: return new DateTime(now.Year, now.Month, 1, 0, 0, 0).AddYears(-1);
                case DateTimeComparer.MonthThis2YearsYearAgo: return new DateTime(now.Year, now.Month, 1, 0, 0, 0).AddYears(-2);
                case DateTimeComparer.MonthLastMonth: return new DateTime(now.Year, now.Month, 1, 0, 0, 0).AddMonths(-1);
                case DateTimeComparer.MonthLastLastYear: return new DateTime(now.Year, now.Month, 1, 0, 0, 0).AddMonths(-1).AddYears(-1);
                case DateTimeComparer.MonthLast2YearsAgo: return new DateTime(now.Year, now.Month, 1, 0, 0, 0).AddMonths(-1).AddYears(-2);
                case DateTimeComparer.QuarterThisQuarter: return new DateTime(now.Year, (((now.Month - 1) / 3) * 3) + 1, 1, 0, 0, 0);
                case DateTimeComparer.QuarterThisLastYear: return new DateTime(now.Year, (((now.Month - 1) / 3) * 3) + 1, 1, 0, 0, 0).AddYears(-1);
                case DateTimeComparer.QuarterThis2YearsAgo: return new DateTime(now.Year, (((now.Month - 1) / 3) * 3) + 1, 1, 0, 0, 0).AddYears(-2);
                case DateTimeComparer.QuarterLastQuarter: return new DateTime(now.Year, (((now.Month - 1) / 3) * 3) + 1, 1, 0, 0, 0).AddMonths(-3);
                case DateTimeComparer.QuarterLastLastYear: return new DateTime(now.Year, (((now.Month - 1) / 3) * 3) + 1, 1, 0, 0, 0).AddMonths(-3).AddYears(-1);
                case DateTimeComparer.QuarterLast2YearsAgo: return new DateTime(now.Year, (((now.Month - 1) / 3) * 3) + 1, 1, 0, 0, 0).AddMonths(-3).AddYears(-2);
                case DateTimeComparer.YearThis: return new DateTime(now.Year, 1, 1, 0, 0, 0);
                case DateTimeComparer.YearLast: return new DateTime(now.Year, 1, 1, 0, 0, 0).AddYears(-1);
                case DateTimeComparer.Year2YearsAgo: return new DateTime(now.Year, 1, 1, 0, 0, 0).AddYears(-2);
                default:
                    throw new InvalidEnumException(d, "Not yet implemented");
            }
        })());

        private DateTime? _dateTimeComparerLT;
        /// <summary>
        /// Note that once value has been calculated it is cached. The object instance in itself should therefore 
        /// only be used in short-lived scenarios.
        /// </summary>
        private DateTime DateTimeComparerLT => _dateTimeComparerLT ?? (DateTime)(_dateTimeComparerLT = new Func<DateTime>(() => {
            var now = DateTime.Now; var d = Value as DateTimeComparer? ?? throw new InvalidObjectTypeException(Value, typeof(DateTimeComparer));
            switch (d) {
                case DateTimeComparer.HourThis:
                case DateTimeComparer.HourLast:
                    return DateTimeComparerGEQ.AddHours(1);
                case DateTimeComparer.DayToday:
                case DateTimeComparer.Day_Yesterday:
                case DateTimeComparer.Day2DaysAgo:
                    return DateTimeComparerGEQ.AddDays(1);
                case DateTimeComparer.MonthThisMonth:
                case DateTimeComparer.MonthThisLastYear:
                case DateTimeComparer.MonthThis2YearsYearAgo:
                case DateTimeComparer.MonthLastMonth:
                case DateTimeComparer.MonthLastLastYear:
                case DateTimeComparer.MonthLast2YearsAgo:
                    return DateTimeComparerGEQ.AddMonths(1);
                case DateTimeComparer.QuarterThisQuarter:
                case DateTimeComparer.QuarterThisLastYear:
                case DateTimeComparer.QuarterThis2YearsAgo:
                case DateTimeComparer.QuarterLastQuarter:
                case DateTimeComparer.QuarterLastLastYear:
                case DateTimeComparer.QuarterLast2YearsAgo:
                    return DateTimeComparerGEQ.AddMonths(3);
                case DateTimeComparer.YearThis:
                case DateTimeComparer.YearLast:
                case DateTimeComparer.Year2YearsAgo:
                    return DateTimeComparerGEQ.AddYears(1);
                default:
                    throw new InvalidEnumException(d, "Not yet implemented");
            }
        })());

        /// <summary>
        /// Improve on use of <see cref="QueryId.ToString"/> (value is meant to be compatible with parser)
        /// 
        /// Note how Value as enum is given without apostrophes. 
        /// </summary>
        /// <returns></returns>
        public override string ToString() => "WHERE " + Key.PToString + " " +
            // Operator.ToMathSymbol()  // NOTE: This would be preferred method. More human readable.
            Operator +                  // NOTE: This is chosen method, makes the resulting URL IIS-safe in order to avoid System.Web.HttpException "A potentially dangerous Request.Path value was detected from the client (<)."
            (Value == null ? " NULL" :
            (Value.GetType().IsEnum ? (" " + Value) :
            (" '" +
            (Value.GetType().Equals(typeof(DateTime)) ? ((DateTime)Value).ToString(Key.A.DateTimeFormat) :
            Value) +
            "'"))); // TODO: Add more object types here (or (even better) find a more generic approach to the general issue of converting objects to string in AgoRapide)

        /// <summary>
        /// TODO: USE ONE COMMON GENERIC METHOD FOR EnrichKey for all QueryId-classes!!!
        /// TODO: IMPLEMENT CLEANER AND CHAINING OF CLEANER
        /// TODO: enumAttribute.Cleaner=
        /// TODO: IMPLEMENT CHAINING OF VALIDATION!
        /// </summary>
        /// <param name="key"></param>
        public new static void EnrichKey(PropertyKeyAttributeEnriched key) =>
            key.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(value, out var retval, out var errorResponse) ?
                    (retval is QueryIdKeyOperatorValue ? /// Note how <see cref="QueryId.TryParse"/> returns base class <see cref="QueryId"/>, therefore only accept the returned value if it is a <see cref="QueryIdKeyOperatorValue"/>
                        ParseResult.Create(key, retval) :
                        ParseResult.Create("Not a valid " + typeof(QueryIdKeyOperatorValue).ToStringShort() + " (found " + retval.GetType().ToStringShort() + ")")
                    ) :
                    ParseResult.Create(errorResponse);
            });
    }
}