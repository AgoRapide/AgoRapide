using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {
    /// <summary>
    /// TODO: To be extended. Parsing for instance does not work for the moment.
    /// </summary>
    [Class(Description = 
        "The general form of -" + nameof(QueryId) + "-. " +
        "Typical example would be : WHERE LastName LIKE 'John%'")]
    public class QueryIdKeyOperatorValue : QueryId {

        /// <summary>
        /// TODO: Check initialization of this
        /// </summary>
        public PropertyKeyAttributeEnriched Key { get; private set; }
        public Operator Operator { get; private set; }
        public object Value { get; private set; }

        protected string _toString;
        /// <summary>
        /// Improve on use of <see cref="QueryId.ToString"/> (value is meant to be compatiable with parser)
        /// </summary>
        /// <returns></returns>
        public override string ToString() => _toString ?? throw new NullReferenceException(nameof(_toString));

        public List<PropertyKeyAttributeEnriched> Properties { get; private set; }

        ///// <summary>
        ///// Constructor for generic parsing of any kind of SQL expression. 
        ///// (usually used when query originates from "outside" of API)
        ///// 
        ///// TODO: IMPLEMENT PARSING HERE!
        ///// </summary>
        ///// <param name="sql">
        ///// SQL WHERE like expression like 
        /////   WHERE first_name LIKE 'John%' 
        ///// or
        /////   WHERE date_of_birth > '2017-01-01' 
        ///// </param>
        //public PropertyValueQueryId(string sql) {
        //    if (!TryParse(value, out var ))
        //}

        /// <summary>
        /// Constructor for "all" query (results in an empty <see cref="QueryId.SQLWhereStatement"/>
        /// </summary>
        public QueryIdKeyOperatorValue() {
            Properties = null;
            Operator = Operator.None;
            Value = null;
            Initialize();
        }

        /// <summary>
        /// TODO: Add a LIMIT parameter to <see cref="QueryIdKeyOperatorValue"/>.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="_operator"></param>
        /// <param name="value"></param>
        public QueryIdKeyOperatorValue(PropertyKeyAttributeEnriched property, Operator _operator, object value) : this(new List<PropertyKeyAttributeEnriched> { property }, _operator, value) { }
        /// <summary>
        /// Strongly typed constructor. 
        /// (usually used when query originates from "outside" of API)
        /// 
        /// TODO: Add a LIMIT parameter to <see cref="QueryIdKeyOperatorValue"/>.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="_operator"></param>
        /// <param name="value"></param>
        public QueryIdKeyOperatorValue(List<PropertyKeyAttributeEnriched> properties, Operator _operator, object value) {
            Properties = properties ?? throw new NullReferenceException(nameof(properties));
            if (Properties.Count == 0) throw new InvalidCountException(nameof(Properties) + ": " + Properties.Count);
            Operator = _operator != Operator.None ? _operator : throw new InvalidEnumException(_operator);
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Initialize();
        }

        /// <summary>
        /// Builds <see cref="QueryId.SQLWhereStatement"/> (together with <see cref="QueryId.SQLWhereStatementParameters"/>) 
        /// based on <see cref="Property"/> <see cref="Operator"/>, <see cref="Value"/>. 
        /// 
        /// Note how <see cref="Value"/> may even be a list, in which case code like 
        ///   WHERE value IN (42, 43) 
        /// will be generated.
        /// 
        /// TODO: Support for List<SomeEnum> is not yet implemented. 
        /// </summary>
        private void Initialize() {

            if (Properties != null && Properties.Count == 1 && Properties[0].A.IsUniqueInDatabase) { /// TODO: Can we use <see cref="Key"/> directly here?
                IsSingle = true;
                IsMultiple = false;
            } else {
                IsSingle = false;
                IsMultiple = true;
            }

            if ((Properties == null || Properties.Count == 0) && Operator == Operator.None && Value == null) {
                // Use empty SQL statement
                _SQLWhereStatement = "";
                _toString = "All"; /// Improve on use of <see cref="QueryId.ToString"/>
                IsAll = true;
                return;
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
                        }
                    }
                    if (Value.GetType().IsEnum) {
                        Operator.AssertValidForType(typeof(string), detailer);
                        sql.Append(DBField.strv + " " + Operator.ToSQLString() + " '" +
                            (PropertyKeyMapper.TryGetA(Value.ToString(), out var key) ? key.Key.PToString : Value.ToString()) +
                            "'");
                    }
                    if (Value is ITypeDescriber) {
                        Operator.AssertValidForType(typeof(string), detailer);
                        parameterNo++;
                        sql.Append(DBField.strv + " " + Operator.ToSQLString() + " :" + DBField.strv + (parameterNo.ToString()));
                        SQLWhereStatementParameters.Add((DBField.strv + (parameterNo.ToString()), Value.ToString()));
                    }
                    if (valueAsList<long>(DBField.lngv) != null) return;
                    if (valueAsList<double>(DBField.dblv) != null) return;
                    if (valueAsList<bool>(DBField.blnv) != null) return;
                    if (valueAsList<DateTime>(DBField.dtmv) != null) return;
                    if (valueAsList<string>(DBField.strv) != null) return;

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

            if (Properties == null || Properties.Count == 0) throw new NullReferenceException(nameof(Properties) + ", details: ??? " + Operator + " " + Value + "(of type " + Value.GetType() + ")");
            if (Properties.Count == 1) {
                _SQLWhereStatement = singlePropertySQLConstructor(Properties[0]);
            } else {
                // TODO: Fix, SQL will look like
                // TODO:   ... AND 
                // TODO:   (
                // TODO:   (key = 'FirstName' AND strv ILIKE :strv1) OR 
                // TODO:   (key = 'LastName' AND strv ILIKE :strv2) OR 
                // TODO:   (key = 'Email' AND strv ILIKE :strv3)
                // TODO:   ) AND 
                // TODO:   ...
                // TODO: Which clearly does not look optimal...
                _SQLWhereStatement = "(\r\n   " + string.Join(" OR\r\n   ", Properties.Select(p => "(" + singlePropertySQLConstructor(p) + ")")) + "\r\n)";
            }

            _toString = ToStringDebug(); /// Improve on use of <see cref="QueryId.ToString"/> (value is meant to be compatiable with parser)
        }

        // Should the property be put in the base-class? 
        // public string SQLWHEREStatement => "WHERE " + DBField.key + " = '" + Property.GetAgoRapideAttribute().PToString;

        /// <summary>
        /// TODO: USE ONE COMMON GENERIC METHOD FOR EnrichAttribute for all QueryId-classes!!!
        /// TODO: IMPLEMENT CLEANER AND CHAINING OF CLEANER
        /// TODO: enumAttribute.Cleaner=
        /// TODO: IMPLEMENT CHAINING OF VALIDATION!
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        public new static void EnrichAttribute(PropertyKeyAttributeEnriched agoRapideAttribute) =>
            agoRapideAttribute.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(value, out var retval, out var errorResponse) ?
                    (retval is QueryIdKeyOperatorValue ? /// <see cref="QueryId.TryParse"/> returns <see cref="QueryId"/> only accept if <see cref="QueryIdKeyOperatorValue"/>
                        ParseResult.Create(agoRapideAttribute, retval) :
                        ParseResult.Create("Not a valid " + typeof(QueryIdKeyOperatorValue).ToStringShort() + " (found " + retval.GetType().ToStringShort() + ")")
                        ) :
                        ParseResult.Create(errorResponse);
            });
    }
}
