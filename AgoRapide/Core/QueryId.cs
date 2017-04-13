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
            "Represents a search term in the API.\r\n" +
            "In its simplest form it is just a long integer that corresponds directly to " + nameof(DBField.id) + " " +
            "(see -" + nameof(QueryIdInteger) + "-). " +
            "It can also be more complex like 'WHERE -" + nameof(CoreP.Name) + "- LIKE 'John%' ORDER BY -" + nameof(DBField.id) + "- " +
            "(see -" + nameof(QueryIdKeyOperatorValue) + "-).",
        LongDescription =
            "This class translates an (untrusted) SQL like expression to a sanitized intermediate format " +
            "that is safe to use in order to build up a real SQL query against the database backend",
        SampleValues = new string[] { "All" },
        InvalidValues = new string[] {
            "WHERE value IN ('A', 'B'" /// TODO: Add more common syntax errors and check that <see cref="TryParse"/> returns good error messages for each kind if syntax error
        }
    )]
    public abstract class QueryId : ITypeDescriber {

        /// <summary>
        /// Note that a <see cref="QueryIdKeyOperatorValue"/> may also be <see cref="IsSingle"/> (for <see cref="AgoRapideAttribute.IsUniqueInDatabase"/>)
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
        /// This should correspond to a value accepted by the corresponding parser (<see cref="TryParse"/>
        /// 
        /// Improve on use of <see cref="QueryId.ToString"/> (value is meant to be compatiable with parser)
        /// </summary>
        /// <returns></returns>
        public abstract override string ToString();
        public string ToStringDebug() => SQLWhereStatement + (SQLWhereStatementParameters.Count == 0 ? "" : "\r\nParameter: ") + string.Join("\r\nParameter: ", SQLWhereStatementParameters.Select(p => p.ToString()));

        /// <summary>
        /// TODO: MAKE INTO <see cref="IEnumerable{object}"/>
        /// 
        /// Always set. Empty if no parameters.
        /// objects are guaranteed to be either of type <see cref="string"/>, type <see cref="double"/> or type <see cref="DateTime"/>
        /// Other types like long, bool, Enums are inserted directly into <see cref="SQLWhereStatement"/>. 
        /// </summary>
        public List<(string key, object value)> SQLWhereStatementParameters { get; protected set; } = new List<(string key, object value)>();
        public string SQLOrderByStatement => ""; // Not yet implemented

        public static QueryId Parse(string value) => TryParse(value, out var retval, out var errorResponse) ? retval : throw new InvalidQueryIdException(nameof(value) + ": " + value + ", " + nameof(errorResponse) + ": " + errorResponse);
        public static bool TryParse(string value, out QueryId id) => TryParse(value, out id, out var dummy);
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

            if (valueToLower.StartsWith("where")) {  // TODO: Implement parsing of WHERE ... format.
                errorResponse = "Invalid long integer, not recognized as " + nameof(QueryIdIdentifier) + " and parsing as " + nameof(QueryIdKeyOperatorValue) + " not yet implemented.";
                id = null;
                return false;
            }

            id = new QueryIdIdentifier(value);
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
        public static void EnrichAttribute(AgoRapideAttributeEnriched agoRapideAttribute) =>
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
        public InvalidCountException(string message, Exception inner) : base(message, inner) { }
    }
}
