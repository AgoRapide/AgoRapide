using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: Consider whether this class is really needed.
    /// </summary>
    public class QueryIdMultiple : QueryId {
        public List<QueryId> Ids;
        public QueryIdMultiple(string ids) : this(ids.Split(",").ToList()) => _toString = ids;
        public QueryIdMultiple(List<string> ids) => ids.Select(id =>
            /// TODO: Because of the use of <see cref="CoreP.IdString"/> 
            /// TODO: <see cref="QueryId.TryParse"/> will most probably never return FALSE. 
            /// TODO: Therefore, change the method's signature into void Parse.
            QueryId.TryParse(id, out var retval) ?
                (!(retval is QueryIdMultiple) ? retval : throw new InvalidObjectTypeException(retval)) :
                throw new NotImplementedException()
        ).ToList();

        public QueryIdMultiple(List<QueryId> ids) {
            if (ids.Count <= 1) throw new InvalidCountException(nameof(ids) + ".Count: " + ids.Count + ". Meaningless / unneccessary complex. Therefore not allowed");
            Ids = ids;

            IsMultiple = true;

            /// TODO: This does not work, we will get something like
            /// TODO: (key = 'IdString' AND strv = :strv1) OR
            /// TODO: (key = 'IdString' AND strv = :strv1) AND
            _SQLWhereStatement = "\r\n(" +
                 string.Join(" OR\r\n", Ids.Select(id => "(" + id.SQLWhereStatement + ")")) +
                 "-- TODO: This SQL code does not work. ids are wrongly named" +
            ")\r\n";
            /// ---------------------------------------------------------
            /// TODO: Solution would be to use 
            /// TODO: key = 'IdString' AND strv IN (:strv1, :strv2, :strv3) and so on.
            /// TODO: and add from <see cref="QueryId.SQLWhereStatementParameters"/> for each 
            /// ---------------------------------------------------------
        }
        private string _toString;
        public override string ToString() => _toString ?? (_toString = string.Join(", ", Ids.Select(id => id.ToString())));
    }
}
