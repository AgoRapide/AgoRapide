using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: Consider merging AggregationType and AggregationKey into <see cref="AgoRapide.AggregationKey"/>
    /// </summary>
    [Class(Description =
        "Querying within a specific context by iterating over two keys like\r\n" +
        "ITERATE Product BY OrderLine.Year.\r\n" +
        "or\r\n" +
        "ITERATE Product BY OrderLine.Year SUM OrderLine.Amount.\r\n" +
        "See -" + nameof(FieldIterator) + "- for details.\r\n" +
        "Both keys must have -" + nameof(PropertyKeyAttribute.HasLimitedRange) + "-")]
    public class QueryIdFieldIterator : QueryId {

        /// <summary>
        /// Note how the RowColumn-concept exists outside of this class, just like <see cref="QueryIdKeyOperatorValue"/> does not know for which TYPE it is used.
        /// </summary>
        public PropertyKey RowKey { get; private set; }
        public Type ColumnType { get; private set; }
        public PropertyKey ColumnKey { get; private set; }

        /// TODO: Consider merging AggregationType and AggregationKey into <see cref="AgoRapide.AggregationKey"/>
        [ClassMember(Description = "Most common value is -" + nameof(AggregationType.Count) + "-, with -" + nameof(AggregationKey) + "- correspondingly set to null.")]
        public AggregationType AggregationType { get; private set; }

        /// TODO: Consider merging AggregationType and AggregationKey into <see cref="AgoRapide.AggregationKey"/>
        [ClassMember(Description = "This is often null (with -" + nameof(AggregationType) + "- correspondingly set to " + nameof(AggregationType.Count) + "-.")]
        public PropertyKey AggregationKey { get; private set; }

        /// <summary>
        /// TODO: Consider merging AggregationType and AggregationKey into <see cref="AgoRapide.AggregationKey"/>
        /// </summary>
        /// <param name="rowKey"></param>
        /// <param name="columnType"></param>
        /// <param name="columnKey"></param>
        /// <param name="aggregationType"></param>
        /// <param name="aggregationKey"></param>
        public QueryIdFieldIterator(PropertyKey rowKey, Type columnType, PropertyKey columnKey, AggregationType aggregationType, PropertyKey aggregationKey) {
            RowKey = rowKey ?? throw new NullReferenceException(nameof(rowKey));
            // rowKey.Key.A.AssertHasLimitedRange();
            ColumnType = columnType ?? throw new NullReferenceException(nameof(columnType));
            ColumnKey = columnKey ?? throw new NullReferenceException(nameof(columnKey));
            // columnKey.Key.A.AssertHasLimitedRange();
            AggregationType = aggregationType;
            if (aggregationKey == null && aggregationType != AggregationType.Count) throw new ArgumentNullException(nameof(aggregationKey) + ". (null is only allowed for -" + nameof(aggregationType) + "- " + nameof(AggregationType.Count) + ", not " + aggregationType);
            AggregationKey = aggregationKey;
            SQLQueryNotPossible = true;
            _SQLWhereStatement = ToString(); /// HACK: Made in order for <see cref="QueryId.Equals"/> to work.
        }

        public override bool IsMatch(BaseEntity entity) => throw new NotImplementedException("Not relevant for " + GetType());
        public override string ToString() => "ITERATE " + RowKey.Key.PToString + " BY " + ColumnType.ToStringVeryShort() + "." + ColumnKey.Key.PToString +
           (AggregationKey == null ? "" : (" " + AggregationType.ToString().ToUpper() + " " + AggregationKey.Key.PToString)); /// Note upper-case for field <see cref="AggregationType"/>

        public static new void EnrichKey(PropertyKeyAttributeEnriched key) => QueryId.EnrichKey(key);
    }
}
