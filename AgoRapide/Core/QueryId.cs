using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: Consider making even more generic, by also adding the entity type...
    /// TODO: (if possible to unite with generic methods though)
    /// 
    /// TODO: Implement in <see cref="TryParse"/> parsing of more advanced variants like:
    ///   WHERE first_name LIKE 'John%' ORDER BY last_name, first_name
    ///   WHERE date_of_birth > '2017-01-01' ORDER BY id DESC
    /// </summary>
    [AgoRapide(
        Description =
            "Represents a search term in the API. " +
            "In its simplest form it is just a long integer that corresponds directly to " + nameof(DBField.id) + ". " +
            "It can also be more complex like 'WHERE -" + nameof(CoreProperty.Name) + "- LIKE 'John%' ORDER BY -" + nameof(DBField.id) + "-",
        LongDescription =
            "This class translates an (untrusted) SQL like expression to a sanitized intermediate format " +
            "that is safe to use in order to build up a real SQL query against the database backend",
        SampleValues = new string[] { "All" },
        InvalidValues = new string[] {
            "WHERE value IN ('A', 'B'" /// TODO: Add more common syntax errors and check that <see cref="TryParse"/> returns good error messages for each kind if syntax error
        }
    )]
    public abstract class QueryId<TProperty> : ITypeDescriber where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"

        /// <summary>
        /// Note that a <see cref="PropertyValueQueryId{TProperty}"/> may also be <see cref="IsSingle"/> (for <see cref="AgoRapideAttribute.IsUniqueInDatabase"/>)
        /// </summary>
        public bool IsSingle { get; protected set; }
        public void AssertIsSingle() {
            if (!IsSingle) throw new InvalidCountException("!" + nameof(IsSingle) + " for " + ToString());
        }
        public bool IsMultiple { get; protected set; }
        public void AssertIsMultiple() {
            if (!IsMultiple) throw new InvalidCountException("!" + nameof(IsMultiple) + " for " + ToString());
        }

        /// <summary>
        /// Returns something like 
        /// "key = 'IsAnonymous' AND blnv = TRUE" or  
        /// "key = 'Name' AND strv LIKE :strv (with corresponding parameter in <see cref="SQLWhereStatementParameters"/>)
        /// 
        /// More advanced versions:
        /// "key = 'AccessRight' AND strv IN ('User', 'Relation', 'Admin')"
        /// "key = 'Name' AND strv IN (:strv1, :strv2, :strv3)"  (with corresponding parameters in <see cref="SQLWhereStatementParameters"/>)
        /// 
        /// May be empty (understood as an "All"-query)
        /// 
        /// Supposed to be combined in a SQL query with filter for a specific type. 
        /// </summary>
        public string SQLWhereStatement { get; protected set; }
        /// <summary>
        /// TODO: Change ToString so that it has the same format as that expected by the parser. 
        /// TODO: (or change parser...)
        /// </summary>
        /// <returns></returns>
        public override string ToString() => SQLWhereStatement + (SQLWhereStatementParameters.Count == 0 ? "" : "\r\nParameter: ") + string.Join("\r\nParameter: ", SQLWhereStatementParameters.Select(p => p.ToString()));

        /// <summary>
        /// TODO: MAKE INTO <see cref="IEnumerable{object}"/>
        /// 
        /// Always set. Empty if no parameters.
        /// objects are guaranteed to be either of type <see cref="string"/>, type <see cref="double"/> or type <see cref="DateTime"/>
        /// Other types like long, bool, Enums are inserted directly into <see cref="SQLWhereStatement"/>. 
        /// </summary>
        public List<Tuple<string, object>> SQLWhereStatementParameters { get; protected set; } = new List<Tuple<string, object>>();
        public string SQLOrderByStatement => ""; // Not yet implemented

        public static QueryId<TProperty> Parse(string value) => TryParse(value, out var retval, out var errorResponse) ? retval : throw new InvalidQueryIdException(nameof(value) + ": " + value + ", " + nameof(errorResponse) + ": " + errorResponse);
        public static bool TryParse(string value, out QueryId<TProperty> id) => TryParse(value, out id, out var dummy);
        public static bool TryParse(string value, out QueryId<TProperty> id, out string errorResponse) {
            if (long.TryParse(value, out var lngId)) {
                id = new IntegerQueryId<TProperty>(lngId);
                errorResponse = null;
                return true;
            }

            if (value.ToLower().Equals("all")) {
                id = new PropertyValueQueryId<TProperty>();
                errorResponse = null;
                return true;
            }

            errorResponse = "Invalid long integer (and parsing as " + nameof(PropertyValueQueryId<TProperty>) + " not yet implemented)";
            id = null;
            return false;
            // throw new NotImplementedException("General parsing. value: " + value);

        }

        /// <summary>
        /// TODO: USE ONE COMMON GENERIC METHOD FOR EnrichAttribute for all QueryId-classes!!!
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        public static void EnrichAttribute(AgoRapideAttributeT<TProperty> agoRapideAttribute) {
            /// TODO: IMPLEMENT CLEANER AND CHAINING OF CLEANER
            /// enumAttribute.Cleaner=
            /// 
            /// TODO: IMPLEMENT CHAINING OF VALIDATION!
            agoRapideAttribute.ValidatorAndParser = new Func<string, ParseResult<TProperty>>(value => {
                return TryParse(value, out var retval, out var errorResponse) ?
                    new ParseResult<TProperty>(new Property<TProperty>(agoRapideAttribute.P, retval), retval) :
                    new ParseResult<TProperty>(errorResponse);
            });
        }

        public class InvalidQueryIdException : ApplicationException {
            public InvalidQueryIdException(string message) : base(message) { }
            public InvalidQueryIdException(string message, Exception inner) : base(message, inner) { }
        }

        /// <summary>
        /// Asserts that the number of entities found is consistens with type of query
        /// </summary>
        /// <param name="count"></param>
        public void AssertCountFound(int count) {
            if (count == 0) return;
            if (IsSingle && count > 1) throw new InvalidCountException(ToString() + " resulted in " + count + " entities found");
        }
    }

    /// <summary>
    /// The simplest form of <see cref="QueryId{TProperty}"/> designating only a single <see cref="DBField.id"/>-value.
    /// </summary>
    /// <typeparam name="TProperty"></typeparam>
    [AgoRapide( // Note how we would have gotten attributes for super class if not defined here
        Description = "The simplest form of -" + nameof(QueryId<CoreProperty>) + "-, accepting only integer id's corresponding to -" + nameof(DBField.id) + "-",
        SampleValues = new string[] { "42" })]
    public class IntegerQueryId<TProperty> : QueryId<TProperty> where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
        public long Id { get; private set; }
        public override string ToString() => Id.ToString();

        public IntegerQueryId(long id) {
            Id = id != 0 ? id : throw new ArgumentException(nameof(id) + ": " + id);
            IsSingle = true;
            IsMultiple = false;
            SQLWhereStatement = "WHERE " + DBField.id + " = " + Id;
        }

        /// <summary>
        /// TODO: USE ONE COMMON GENERIC METHOD FOR EnrichAttribute for all QueryId-classes!!!
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        public new static void EnrichAttribute(AgoRapideAttributeT<TProperty> agoRapideAttribute) {
            /// TODO: IMPLEMENT CLEANER AND CHAINING OF CLEANER
            /// enumAttribute.Cleaner=
            /// 
            /// TODO: IMPLEMENT CHAINING OF VALIDATION!
            agoRapideAttribute.ValidatorAndParser = new Func<string, ParseResult<TProperty>>(value => {
                return TryParse(value, out var retval, out var errorResponse) ?
                    (retval is IntegerQueryId<TProperty> ? /// <see cref="QueryId{TProperty}.TryParse"/> returns <see cref="QueryId{TProperty}"/> only accept if <see cref="IntegerQueryId{TProperty}"/>
                    new ParseResult<TProperty>(new Property<TProperty>(agoRapideAttribute.P, retval), retval) :
                        new ParseResult<TProperty>("Not a valid " + typeof(IntegerQueryId<TProperty>).ToStringShort() + " (found " + retval.GetType().ToStringShort() + ")")
                        ) :
                        new ParseResult<TProperty>(errorResponse);
            });
        }
    }

    /// <summary>
    /// The general form of <see cref="QueryId{TProperty}"/>.
    /// 
    /// TODO: To be extended. Parsing for instance does not work for the moment.
    /// 
    /// Note how this class gets the attributes for the super class since no <see cref="AgoRapideAttribute"/> is defined here. 
    /// </summary>
    /// <typeparam name="TProperty"></typeparam>
    public class PropertyValueQueryId<TProperty> : QueryId<TProperty> where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
        public List<TProperty> Properties { get; private set; }
        public AgoRapideAttributeT<TProperty> A { get; private set; }
        public Operator Operator { get; private set; }
        public object Value { get; private set; }

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
        /// Constructor for "all" query (results in an empty <see cref="QueryId{TProperty}.SQLWhereStatement"/>
        /// </summary>
        public PropertyValueQueryId() {
            Properties = null;
            Operator = Operator.None;
            Value = null;
            Initialize();
        }

        /// <summary>
        /// TODO: Add a LIMIT parameter to <see cref="PropertyValueQueryId{TProperty}"/>.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="_operator"></param>
        /// <param name="value"></param>
        public PropertyValueQueryId(TProperty property, Operator _operator, object value) : this(new List<TProperty> { property }, _operator, value) { }
        /// <summary>
        /// Strongly typed constructor. 
        /// (usually used when query originates from "outside" of API)
        /// 
        /// TODO: Add a LIMIT parameter to <see cref="PropertyValueQueryId{TProperty}"/>.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="_operator"></param>
        /// <param name="value"></param>
        public PropertyValueQueryId(List<TProperty> properties, Operator _operator, object value) {
            Properties = properties ?? throw new NullReferenceException(nameof(properties));
            if (Properties.Count == 0) throw new InvalidCountException(nameof(Properties) + ": " + Properties.Count);
            Operator = _operator != Operator.None ? _operator : throw new InvalidEnumException(_operator);
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Initialize();
        }

        /// <summary>
        /// Builds <see cref="QueryId{TProperty}.SQLWhereStatement"/> (together with <see cref="QueryId{TProperty}.SQLWhereStatementParameters"/>) 
        /// based on <see cref="Property"/> <see cref="Operator"/>, <see cref="Value"/>. 
        /// 
        /// Note how <see cref="Value"/> may even be a list, in which case code like 
        ///   WHERE value IN (42, 43) 
        /// will be generated.
        /// 
        /// TODO: Support for List<SomeEnum> is not yet implemented. 
        /// </summary>
        private void Initialize() {

            if (Properties != null && Properties.Count == 1 && Properties[0].GetAgoRapideAttribute().A.IsUniqueInDatabase) {
                IsSingle = true;
                IsMultiple = false;
            } else {
                IsSingle = false;
                IsMultiple = true;
            }

            if ((Properties == null || Properties.Count == 0) && Operator == Operator.None && Value == null) {
                // Use empty SQL statement
                SQLWhereStatement = "";
                return;
            }

            /// "Normally" we expect there to be only one parameter. 
            /// We have to number parameters for instance for <see cref="Operator.IN"/> and for multiple <see cref="Properties"/>
            var parameterNo = 0;

            var singlePropertySQLConstructor = new Func<TProperty, string>(Property => {
                var sql = new StringBuilder();
                A = Property.GetAgoRapideAttribute();
                var detailer = new Func<string>(() => A.PToString + " " + Operator + " " + Value + " (of type " + Value.GetType() + ")");

                if (((int)(object)(Property)) == 0) throw new InvalidEnumException(Property, detailer.Result("Details: "));

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
                                SQLWhereStatementParameters.Add(new Tuple<string, object>(dbField + (parameterNo).ToString(), retval));
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
                                    SQLWhereStatementParameters.Add(new Tuple<string, object>(dbField + (parameterNo).ToString(), v));
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
                        var _string = Value as string;
                        if (_string != null) {
                            Operator.AssertValidForType(typeof(string), detailer);
                            parameterNo++;
                            sql.Append(DBField.strv + " " + Operator.ToSQLString() + " :" + DBField.strv + (parameterNo).ToString());
                            SQLWhereStatementParameters.Add(new Tuple<string, object>(DBField.strv + (parameterNo).ToString(), _string));
                            return;
                        }
                    }
                    if (Value.GetType().IsEnum) {
                        Operator.AssertValidForType(typeof(string), detailer);
                        var a = new AgoRapideAttributeT<TProperty>(Value.GetAgoRapideAttribute());
                        sql.Append(DBField.strv + " " + Operator.ToSQLString() + " '" + a.PToString + "'");
                    }
                    if (Value is ITypeDescriber) {
                        Operator.AssertValidForType(typeof(string), detailer);
                        parameterNo++;
                        sql.Append(DBField.strv + " " + Operator.ToSQLString() + " :" + DBField.strv + (parameterNo.ToString()));
                        SQLWhereStatementParameters.Add(new Tuple<string, object>(DBField.strv + (parameterNo.ToString()), Value.ToString()));
                    }
                    if (valueAsList<long>(DBField.lngv) != null) return;
                    if (valueAsList<double>(DBField.dblv) != null) return;
                    if (valueAsList<bool>(DBField.blnv) != null) return;
                    if (valueAsList<DateTime>(DBField.dtmv) != null) return;
                    if (valueAsList<string>(DBField.strv) != null) return;

                    // TODO: Support for List<SomeEnum> is not yet implemented. 
                    // 
                    // This would be too naive:
                    //   if (valueAsList<TProperty>(DBField.strv) != null) return;
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
                SQLWhereStatement = singlePropertySQLConstructor(Properties[0]);
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
                SQLWhereStatement = "(\r\n   " + string.Join(" OR\r\n   ", Properties.Select(p => "(" + singlePropertySQLConstructor(p) + ")")) + "\r\n)";
            }
        }

        // Should the property be put in the base-class? 
        // public string SQLWHEREStatement => "WHERE " + DBField.key + " = '" + Property.GetAgoRapideAttribute().PToString;

        /// <summary>
        /// TODO: USE ONE COMMON GENERIC METHOD FOR EnrichAttribute for all QueryId-classes!!!
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        public new static void EnrichAttribute(AgoRapideAttributeT<TProperty> agoRapideAttribute) {
            /// TODO: IMPLEMENT CLEANER AND CHAINING OF CLEANER
            /// enumAttribute.Cleaner=
            /// 
            /// TODO: IMPLEMENT CHAINING OF VALIDATION!
            agoRapideAttribute.ValidatorAndParser = new Func<string, ParseResult<TProperty>>(value => {
                return TryParse(value, out var retval, out var errorResponse) ?
                    (retval is PropertyValueQueryId<TProperty> ? /// <see cref="QueryId{TProperty}.TryParse"/> returns <see cref="QueryId{TProperty}"/> only accept if <see cref="PropertyValueQueryId{TProperty}"/>
                        new ParseResult<TProperty>(new Property<TProperty>(agoRapideAttribute.P, retval), retval) :
                        new ParseResult<TProperty>("Not a valid " + typeof(PropertyValueQueryId<TProperty>).ToStringShort() + " (found " + retval.GetType().ToStringShort() + ")")
                        ) :
                        new ParseResult<TProperty>(errorResponse);
            });
        }
    }

    /// <summary>
    /// TODO: Support IS (like IS NULL)
    /// 
    /// TODO: Move somewhere better! To Enum-folder most probably.
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

    /// <summary>
    /// TODO: Move somewhere better!
    /// </summary>
    public class InvalidCountException : ApplicationException {
        public InvalidCountException(string message) : base(message) { }
        public InvalidCountException(string message, Exception inner) : base(message, inner) { }
    }
}
