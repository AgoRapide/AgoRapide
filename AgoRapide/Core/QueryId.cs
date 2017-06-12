// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;
using AgoRapide.API;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: Consider making even more generic, by also adding the entity type...
    /// TODO: (if possible to unite with generic methods though)
    /// 
    /// TODO: Implement in <see cref="TryParse"/> parsing of more advanced variants like:
    ///   WHERE first_name LIKE 'John%' ORDER BY last_name, first_name
    ///   WHERE date_of_birth > '2017-01-01' ORDER BY id DESC
    /// </summary>
    [Class(
        Description =
            "Represents a search term in the API.\r\n" +
            "Also used as a general identifier usable in URLs and similar.\r\n" +
            "(-" + nameof(QueryIdString) + "- is a human friendly alternative to -" + nameof(BaseEntity.Id) + "-.)r\n" +
            "In its simplest form it is just a long integer that corresponds directly to " + nameof(DBField.id) + " " +
            "(see -" + nameof(QueryIdInteger) + "-).\r\n" +
            "It can also be more complex like 'WHERE -" + nameof(CoreP.IdFriendly) + "- LIKE 'John%' ORDER BY -" + nameof(DBField.id) + "- " +
            "(see -" + nameof(QueryIdKeyOperatorValue) + "-).\r\n",
        LongDescription =
            "This class does NOT contain information about what type of entity is being queried.\r\n" +
            "This class translates an (untrusted) SQL-like expression to a sanitized intermediate format " +
            "that is safe to use in order to build up a real SQL query against the database backend.\r\n" +
            "Sub classes:\r\n" +
            "-" + nameof(QueryIdInteger) + "-\r\n" +
            "-" + nameof(QueryIdKeyOperatorValue) + "-\r\n" +
            "-" + nameof(QueryIdString) + "-\r\n" +
            "-" + nameof(QueryIdMultiple) + "-",
        SampleValues = new string[] {
            "All",
            "WHERE " + nameof(EntityTypeCategory) + " = " + nameof(EntityTypeCategory.APIDataObject) /// = corresponds to <see cref="Operator.EQ"/>
        },
        InvalidValues = new string[] {
            "WHERE value IN ('A', 'B'" /// TODO: Add more common syntax errors and check that <see cref="TryParse"/> returns good error messages for each kind if syntax error
        }
    )]
    public abstract class QueryId : ITypeDescriber {

        /// <summary>
        /// Note that a <see cref="QueryIdKeyOperatorValue"/> may also be <see cref="IsSingle"/> (for <see cref="PropertyKeyAttribute.IsUniqueInDatabase"/>)
        /// </summary>
        public bool IsSingle { get; protected set; }
        public void AssertIsSingle() {
            if (!IsSingle) throw new InvalidCountException("!" + nameof(IsSingle) + " for " + ToString());
        }
        public void AssertIsSingle(Func<string> detailer) {
            if (!IsSingle) throw new InvalidCountException("!" + nameof(IsSingle) + " for " + ToString() + detailer.Result("\r\nDetails: "));
        }

        public bool IsMultiple { get; protected set; }
        public void AssertIsMultiple() {
            if (!IsMultiple) throw new InvalidCountException("!" + nameof(IsMultiple) + " for " + ToString());
        }
        public void AssertIsMultiple(Func<string> detailer) {
            if (!IsMultiple) throw new InvalidCountException("!" + nameof(IsMultiple) + " for " + ToString() + detailer.Result("\r\nDetails: "));
        }

        [ClassMember(Description = "Corresponds to -" + nameof(ToString) + "- returning \"All\"")]
        public bool IsAll { get; protected set; }

        protected string _SQLWhereStatement;
        [ClassMember(
            Description =
                "Trusted \"safe to use\" value.\r\n" +
                "Returns something like\r\n" +
                "\"key = 'IsAnonymous' AND blnv = TRUE\" or\r\n" +
                "\"key = 'Name' AND strv LIKE :strv\" (with corresponding parameter in -" + nameof(SQLWhereStatementParameters) + "-)\r\n" +
                "Supposed to be combined in a SQL query with filter for a specific type.\r\n" +
                "May be empty (understood as an \"All\" - query)",
            LongDescription =
                "More advanced versions:\r\n" +
                "\"key = 'AccessRight' AND strv IN ('User', 'Relation', 'Admin')\"\r\n" +
                "\"key = 'Name' AND strv IN (:strv1, :strv2, :strv3)\" (with corresponding parameters in -" + nameof(SQLWhereStatementParameters) + "-)"
        )]
        public string SQLWhereStatement => _SQLWhereStatement ?? throw new NullReferenceException(nameof(SQLWhereStatement) + ". Should have been set by sub class");

        /// <summary>
        /// TODO: MAKE INTO <see cref="IEnumerable{object}"/>
        /// 
        /// Always set. Empty if no parameters.
        /// objects are guaranteed to be either of type <see cref="string"/>, type <see cref="double"/> or type <see cref="DateTime"/>
        /// Other types like long, bool, Enums are inserted directly into <see cref="SQLWhereStatement"/>. 
        /// </summary>
        public List<(string key, object value)> SQLWhereStatementParameters { get; protected set; } = new List<(string key, object value)>();
        public string SQLOrderByStatement => ""; // Not yet implemented

        /// <summary>
        /// This should correspond to a value accepted by the corresponding parser (<see cref="TryParse"/>
        /// 
        /// Improve on use of <see cref="QueryId.ToString"/> (value is meant to be compatiable with parser)
        /// </summary>
        /// <returns></returns>
        public abstract override string ToString();
        public string ToStringDebug() => SQLWhereStatement + (SQLWhereStatementParameters.Count == 0 ? "" : "\r\nParameter: ") + string.Join("\r\nParameter: ", SQLWhereStatementParameters.Select(p => p.ToString()));

        // public abstract string ToAPIQuery(); /// Unnecessary, equivalent to ToString()

        public static QueryId Parse(string value) => TryParse(value, out var retval, out var errorResponse) ? retval : throw new InvalidQueryIdException(nameof(value) + ": " + value + ", " + nameof(errorResponse) + ": " + errorResponse);
        public static bool TryParse(string value, out QueryId id) => TryParse(value, out id, out var dummy);
        /// <summary>
        /// TODO: Because of the use of <see cref="CoreP.IdString"/> 
        /// TODO: <see cref="QueryId.TryParse"/> will most probably never return FALSE. 
        /// TODO: Therefore, change the method's signature into void Parse.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="id"></param>
        /// <param name="errorResponse"></param>
        /// <returns></returns>
        public static bool TryParse(string value, out QueryId id, out string errorResponse) {
            if (long.TryParse(value, out var lngId)) {
                id = new QueryIdInteger(lngId);
                errorResponse = null;
                return true;
            }

            var valueToLower = value.ToLower();
            if (valueToLower.Equals("all")) {
                id = new QueryIdKeyOperatorValue();
                errorResponse = null;
                return true;
            }

            if (valueToLower.StartsWith("where")) {  // TODO: Improve on this parsing
                value = value.Replace("%3D", "="); // HACK: FIX THIS!

                var pos = 0;
                value += " "; // Simplifies parsing
                var nextWord = new Func<string>(() => {
                    var nextPos = value.IndexOf(' ', pos);
                    if (nextPos == -1)
                        return null;
                    var word = value.Substring(pos, nextPos - pos);
                    pos = nextPos + 1;
                    return word;
                });

                nextWord(); var 
                    
                strKey = nextWord();
                if (strKey == null) {
                    id = null;
                    errorResponse = "No key given";
                    return false;
                }
                if (!PropertyKeyMapper.TryGetA(strKey, out var key)) {
                    id = null;
                    errorResponse = "Invalid key (" + strKey + ")";
                    return false;
                }

                var strOperator = nextWord(); Operator _operator;
                if (strOperator == null) {
                    id = null;
                    errorResponse = "No operator given";
                    return false;
                }
                switch (strOperator) {
                    case "=": _operator = Operator.EQ; break;
                    default:
                        id = null;
                        errorResponse = "Invalid operator (" + strOperator + ")";
                        return false;
                }

                var strValue = nextWord();
                if (strValue == null) {
                    id = null;
                    errorResponse = "No value given";
                    return false;
                }
                if (strValue.StartsWith("'") && strValue.EndsWith("'")) strValue = strValue.Substring(1, strValue.Length - 2);

                if (!key.Key.TryValidateAndParse(strValue, out var valueResult)) {
                    id = null;
                    errorResponse = "Invalid value given for " + key.Key.PToString + ".\r\nDetails: " + valueResult.ErrorResponse;
                    return false;
                }

                id = new QueryIdKeyOperatorValue(key.Key, _operator, valueResult.Result.Value);
                errorResponse = null;
                return true;

                //var s = "WHERE " + CoreP.QueryIdParent + " = '";
                //if (value.StartsWith(s)) {
                //    // TODO: IMPLEMENT MORE COMPLETE PARSER HERE!
                //    id = new QueryIdKeyOperatorValue(CoreP.QueryIdParent.A().Key, Operator.EQ, value.Substring(s.Length, value.Length - s.Length - 1));
                //    errorResponse = null;
                //    return true;
                //}
                //errorResponse = "Invalid long integer, not recognized as " + nameof(QueryIdString) + " and parsing as " + nameof(QueryIdKeyOperatorValue) + " not yet implemented fully.";
                //id = null;
                //return false;
            }

            var t = value.ToLower().Split(",");
            if (t.Count > 1) {
                id = new QueryIdMultiple(t);
                errorResponse = null;
                return true;
            }

            if (Documentator.Keys.TryGetValue(value, out var list)) {
                /// There is an attempt to use a friendly id which might not be an <see cref="CoreP.IdString"/>. 
                /// This would typically be the case if we are called from <see cref="APICommandCreator"/>, 
                /// that is, NOT from a API client as a result of an actual query, 
                /// but from "inside" the API in order to generate an API URL.
                switch (list.Count) {
                    case 0: throw new InvalidCountException(nameof(list) + ". Expected at least 1 item in list");
                    case 1:
                        var singleEntity = list[0].Entity;
                        id = singleEntity.IdString;
                        //id = singleEntity.IdString.Equals(singleEntity.Id.ToString()) ?
                        //    (QueryId)new QueryIdInteger(singleEntity.Id) : // A bit surprising, but really no problem 
                        //    (QueryId)new QueryIdString(singleEntity.IdString);  /// Replace with <see cref="CoreP.IdString"/> since that one also works against the database
                        errorResponse = null;
                        return true;
                    default:
                        id = new QueryIdMultiple(list.Select(e => e.Entity.IdString).ToList());
                        errorResponse = null;
                        return true;
                }
            }

            if (!InvalidIdentifierException.TryAssertValidIdentifier(value, out errorResponse)) {
                errorResponse = "Unable to parse as " + nameof(QueryIdString) + ". Details:\r\n*" + errorResponse;
                id = null;
                return false;
            }

            id = new QueryIdString(value);
            errorResponse = null;
            return true;

            // throw new NotImplementedException("General parsing. value: " + value);
        }

        /// <summary>
        /// TODO: USE ONE COMMON GENERIC METHOD FOR EnrichAttribute for all QueryId-classes!!!
        /// TODO: IMPLEMENT CLEANER AND CHAINING OF CLEANER
        /// enumAttribute.Cleaner=
        /// 
        /// TODO: IMPLEMENT CHAINING OF VALIDATION!
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        public static void EnrichAttribute(PropertyKeyAttributeEnriched agoRapideAttribute) =>
            agoRapideAttribute.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(value, out var retval, out var errorResponse) ?
                    ParseResult.Create(agoRapideAttribute, retval) :
                    ParseResult.Create(errorResponse);
            });

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
    /// TODO: Move somewhere better!
    /// </summary>
    public class InvalidCountException : ApplicationException {
        public InvalidCountException(string message) : base(message) { }
        public InvalidCountException(long found, long expected) : base(nameof(expected) + ": " + expected + ", " + nameof(found) + ": " + found) { }
        public InvalidCountException(long found, long expected, string details) : base(nameof(expected) + ": " + expected + ", " + nameof(found) + ": " + found + "\r\nDetails: " + details) { }
        public InvalidCountException(string message, Exception inner) : base(message, inner) { }
    }
}
