using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide;

namespace AgoRapide.API {

    /// <summary>
    /// Note how care has been taken in order to ensure that this class can behave just like any another <see cref="BaseEntity"/>-class regarding
    /// presentation as HTML / JSON / CSV. 
    /// (but note how some methods like <see cref="ToHTMLTableColumns"/> have been overrided from their basic implementation in <see cref="BaseEntity"/>)
    /// 
    /// TODO: Note that <see cref="ResponseFormat.JSON"/> is currently missing <see cref="LeftmostColumn"/>
    /// TOOD: Consider adding it to <see cref="BaseEntity.Properties"/> in <see cref="FieldIterator.FieldIterator"/>
    /// TODO: in order to NOT having to override <see cref="BaseEntity.ToJSONEntity"/>
    /// </summary>
    [Class(Description =
        "Contains functionality for two-dimensional iteration over fields (as given by -" + nameof(QueryIdFieldIterator) + "-), like Sales per Product per Year.\r\n" +
        "Each instance of -" + nameof(FieldIterator) + "- is a row in the resulting table.\r\n" +
        "Each instance is either\r\n" + // TODO: Decide if this is correct? Aggregates can be handled by "NORMAL" mechanism instead.
        "1) An aggregate like -" + nameof(AggregationType.Sum) + "- / -" + nameof(AggregationType.Median) + "- (or similar)\r\n" +
        "or\r\n" +
        "2) A -" + nameof(DrillDownSuggestion) + "- like 'Product = WidgetXYZ' (or similar).\r\n +" +
        "-" + nameof(LeftmostColumn) + "- is set correspondingly.")]
    public class FieldIterator : BaseEntity {

        [ClassMember(Description =
            "The actual values to show.\r\n" +
            "Some elements may be null.\r\n" +
            "Note that for a given collection of -" + nameof(FieldIterator) + "-, all -" + nameof(Columns) + "--collections should have the same number of elements in the same order"
        )]
        public List<Property> Columns { get; private set; }

        [ClassMember(Description = "Something like Sum, Median or 'Product = WidgetXYZ'")]
        public string LeftmostColumn { get; private set; }

        public FieldIterator(string leftmostColumn, List<Property> columns) {
            LeftmostColumn = leftmostColumn ?? throw new ArgumentNullException(nameof(leftmostColumn));
            Columns = columns ?? throw new ArgumentNullException(nameof(columns));
            Properties = new System.Collections.Concurrent.ConcurrentDictionary<CoreP, Property>();
            /// Populate <see cref="BaseEntity.Properties"/>.
            /// Note how class does not use <see cref="BaseEntity.Properties"/> "itself" but by populating it, methods like
            /// <see cref="PropertyKeyAggregate.CalculateSingleValue"/> becomes able to work also on instances of this class.
            Columns.Where(c => c != null).ForEach(c => Properties.AddValue(c.Key.Key.CoreP, c));
        }

        public override List<PropertyKey> ToHTMLTableColumns(Request request) => Columns.Select(p => (PropertyKey)p.Key).ToList();

        public override string ToHTMLTableRowHeading(Request request, List<AggregationType> aggregateRows) {
            // Note how this has no cache because the columns will differ between instances.
            var headers = "<tr><th>&nbsp;</th>" + string.Join("", Columns.Select(p => "<th>" + p.Key.ToHTMLTableHeader() + "</th>")) + "</tr>";
            aggregateRows = aggregateRows?.Where(a => ToHTMLTableColumns(request).Where(t => t.Key.A.AggregationTypes.Any(ka => ka == a)).Count() > 0).ToList() ?? null;

            return "<thead>" +
                (aggregateRows == null || aggregateRows.Count == 0 ? "" : (
                    headers + // Note how field names (headers) are repeated multiple times. TODO: Make better when very few aggregations / contexts suggestions.
                    string.Join("", aggregateRows.Select(a => {
                        return "<tr><th>" + a + "</th>" +
                            string.Join("", ToHTMLTableColumns(request).Select(key => "<th align=\"right\">" +
                                (key.Key.A.AggregationTypes.Contains(a) ? ("<!--" + key.Key.PToString + "_" + a + "-->") : "") +
                                "</th>")) +
                        "</tr>";
                    }))
                )) +
                headers + // Note how field names (headers) are repeated multiple times. TODO: Make better when very few aggregations / contexts suggestions.
                "</thead>";
        }

        public override string ToHTMLTableRow(Request request) => "<tr><td>" +
            LeftmostColumn.HTMLEncode() + "</td>" + string.Join("", Columns.Select(p => "<td" + (p.Key.Key.A.NumberFormat == NumberFormat.None ? "" : " align=\"right\"") + ">" +
                p.V<Property.HTML>().ToString() + "</td>"));

        public override string ToHTMLDetailed(Request request) => throw new NotImplementedException("Would be rather meaningless to implement anyway.");
    
        public override List<PropertyKey> ToCSVTableColumns(Request request) => ToHTMLTableColumns(request);

        public override string ToCSVTableRow(Request request) => LeftmostColumn + request.CSVFieldSeparator + 
            string.Join(request.CSVFieldSeparator, Columns.Select(p => GetCSVPropertyValue(request, p))) + "\r\n";

        public override string ToCSVDetailed(Request request) => throw new NotImplementedException("Would be rather meaningless to implement anyway.");
    }
}