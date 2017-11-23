using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;

namespace AgoRapide.Core {

    [Class(Description = "Querying within a specific context by iterating over two keys like ITERATE Product BY Year. See -" + nameof(FieldIterator) + "- for details.")]
    public class QueryIdFieldIterator : QueryId {

        /// <summary>
        /// Note how the RowColumn-concept exists outside of this class, just like <see cref="QueryIdKeyOperatorValue"/> does not know for which TYPE it is used.
        /// </summary>
        public PropertyKey RowKey { get; private set; }
        public Type ColumnType { get; private set; }
        public PropertyKey ColumnKey { get; private set; }

        public QueryIdFieldIterator(PropertyKey rowKey, Type columnType, PropertyKey columnKey) {
            RowKey = rowKey;
            ColumnType = columnType;
            ColumnKey = columnKey;
        }

        public override bool IsMatch(BaseEntity entity) => throw new NotImplementedException("Not relevant for " + GetType());
        public override string ToString() => "ITERATE " + RowKey.Key.PToString + " BY " + ColumnType.ToStringVeryShort() + "." + ColumnKey.Key.PToString;
    }
}
