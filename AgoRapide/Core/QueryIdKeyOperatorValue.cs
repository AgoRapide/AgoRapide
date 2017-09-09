// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
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

        ///// <summary>
        ///// TODO: Document better reason for both <see cref="Key"/> and <see cref="Properties"/>. 
        ///// </summary>
        //public List<PropertyKeyAttributeEnriched> Properties { get; private set; }

        public Operator Operator { get; private set; }
        public object Value { get; private set; }

        // TODO: REMOVE COMMENT OUT CODE:
        //  _toString ?? throw new NullReferenceException(nameof(_toString));

        // TODO: REMOVE COMMENT OUT CODE:
        ///// <summary>
        ///// Constructor for "all" query (results in an empty <see cref="QueryId.SQLWhereStatement"/>
        ///// </summary>
        //public QueryIdKeyOperatorValue() {
        //    Properties = null;
        //    Operator = Operator.None;
        //    Value = null;
        //    _toString = "All"; /// TODO: Improve on use of <see cref="QueryId.ToString"/>
        //    Initialize();
        //}

        // TODO: REMOVE COMMENT OUT CODE:
        ///// <summary>
        ///// Strongly typed constructor. 
        ///// (usually used when query originates from "outside" of API)
        ///// 
        ///// TODO: Add a LIMIT parameter to <see cref="QueryIdKeyOperatorValue"/>.
        ///// </summary>
        ///// <param name="property"></param>
        ///// <param name="_operator"></param>
        ///// <param name="value"></param>
        //public QueryIdKeyOperatorValue(List<PropertyKeyAttributeEnriched> properties, Operator _operator, object value) {
        //    Properties = properties ?? throw new NullReferenceException(nameof(properties));
        //    if (Properties.Count == 0) throw new InvalidCountException(nameof(Properties) + ": " + Properties.Count);
        //    Operator = _operator != Operator.None ? _operator : throw new InvalidEnumException(_operator);
        //    Value = value ?? throw new ArgumentNullException(Util.BreakpointEnabler + nameof(value)); /// TODO: Add support in <see cref="QueryIdKeyOperatorValue"/> for value null.
        //    switch (properties.Count) {
        //        case 1:
        //            _toString = "WHERE " + properties[0].PToString + " = '" + value + "'";
        //            Key = properties[0];
        //            break; /// TODO: Improve on use of <see cref="QueryId.ToString"/>
        //        default: throw new NotImplementedException(nameof(properties.Count) + ": " + properties.Count + " (" + string.Join(",", properties.Select(p => p.PToString)) + ")");
        //    }
        //    Initialize();
        //}

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
            Value = value ?? throw new ArgumentNullException(Util.BreakpointEnabler + nameof(value)); /// TODO: Add support in <see cref="QueryIdKeyOperatorValue"/> for value null.

            // TODO: REMOVE COMMENT OUT CODE:
            // _toString = "WHERE " + key.PToString + " = '" + value + "'";

            if (Operator == Operator.EQ && Key.A.IsUniqueInDatabase) {
                IsSingle = true;
                IsMultiple = false;
            } else {
                IsSingle = false;
                IsMultiple = true;
            }

            // TODO: REMOVE COMMENT OUT CODE:
            //if ((Properties == null || Properties.Count == 0) && Operator == Operator.None && Value == null) {
            //    // Use empty SQL statement
            //    _SQLWhereStatement = "";
            //    IsAll = true;
            //    return;
            //}

            /// "Normally" we expect there to be only one parameter. 
            /// We have to number parameters for instance for <see cref="Operator.IN"/> and for multiple <see cref="Properties"/>
            var parameterNo = 0;

            var singlePropertySQLConstructor = new Func<PropertyKeyAttributeEnriched, string>(A => {
                var sql = new StringBuilder();
                var detailer = new Func<string>(() => A.PToString + " " + Operator + " " + Value + " (of type " + Value.GetType() + ")");

                if (((int)(object)(A.CoreP)) == 0) throw new InvalidEnumException(A.CoreP, detailer.Result("Details: "));

                sql.Append(DBField.key + " = '" + A.PToString + "' AND ");

                /// Builds SQL query if Value corresponds to T
                T? valueAs<T>(DBField dbField) where T : struct {
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

            // if (Properties.Count == 1) {
            _SQLWhereStatement = singlePropertySQLConstructor(key);

            // TODO: REMOVE COMMENT OUT CODE:
            //} else {
            //    /// TODO: Check connection / overlap of functionality with <see cref="QueryIdKeyOperatorValue"/> with multiple properties and <see cref="QueryIdMultiple"/>
            //    /// TODO: See <see cref="QueryIdKeyOperatorValue.Initialize"/>

            //    // TODO: Fix, SQL will look like
            //    // TODO:   ... AND 
            //    // TODO:   (
            //    // TODO:   (key = 'FirstName' AND strv ILIKE :strv1) OR 
            //    // TODO:   (key = 'LastName' AND strv ILIKE :strv2) OR 
            //    // TODO:   (key = 'Email' AND strv ILIKE :strv3)
            //    // TODO:   ) AND 
            //    // TODO:   ...
            //    // TODO: Which clearly does not look optimal...
            //    _SQLWhereStatement = "(\r\n   " + string.Join(" OR\r\n   ", Properties.Select(p => "(" + singlePropertySQLConstructor(p) + ")")) + "\r\n)";
            //}
        }

        /// <summary>
        /// TODO: Implement for more than <see cref="Operator.EQ"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool IsMatch(BaseEntity entity) {
            if (Key == null) throw new NullReferenceException(nameof(Key) + ". Details: " + ToString());
            if (entity.Properties == null) return false;
            if (!entity.Properties.TryGetValue(Key.CoreP, out var p)) return false;
            switch (Value) {
                case Percentile percentile:
                    if (!p.PercentileIsSet) throw new Property.InvalidPropertyException("!" + nameof(p.PercentileIsSet) + " for " + p.ToString() + ".\r\n" + nameof(entity) + ": " + entity.ToString());
                    switch (Operator) {
                        case Operator.LT: return percentile.Value < p.Percentile.Value;
                        case Operator.LEQ: return percentile.Value <= p.Percentile.Value;
                        case Operator.EQ: return percentile.Value == p.Percentile.Value;
                        case Operator.GEQ: return percentile.Value >= p.Percentile.Value;
                        case Operator.GT: return percentile.Value > p.Percentile.Value;
                        default: throw new InvalidEnumException(Operator);
                    }
                default:
                    switch (Operator) {
                        case Operator.EQ:
                            return p.Value.Equals(Value);
                        default: throw new NotImplementedException(nameof(Operator) + ": " + Operator);
                    }
            }
        }

        /// <summary>
        /// Improve on use of <see cref="QueryId.ToString"/> (value is meant to be compatible with parser)
        /// </summary>
        /// <returns></returns>
        public override string ToString() => "WHERE " + Key.PToString + " = '" + Value + "'";

        /// <summary>
        /// TODO: USE ONE COMMON GENERIC METHOD FOR EnrichAttribute for all QueryId-classes!!!
        /// TODO: IMPLEMENT CLEANER AND CHAINING OF CLEANER
        /// TODO: enumAttribute.Cleaner=
        /// TODO: IMPLEMENT CHAINING OF VALIDATION!
        /// </summary>
        /// <param name="key"></param>
        public new static void EnrichAttribute(PropertyKeyAttributeEnriched key) =>
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