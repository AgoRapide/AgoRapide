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
    /// 
    /// TODO: Add presentation as JSON / CSV.
    /// </summary>
    [Class(Description =
        "Contains functionality for two-dimensional iteration over fields (as given by -" + nameof(QueryIdFieldIterator) + "-), like Sales per Product per Year.\r\n" +
        "Each instance of -" + nameof(FieldIterator) + "- is a row in the resulting table.\r\n" +
        "Each instance is either\r\n" +
        "1) An aggregate like -" + nameof(AggregationType.Sum) + "- / -" + nameof(AggregationType.Median) + "- (or similar)\r\n" +
        "or\r\n" +
        "2) A -" + nameof(DrillDownSuggestion) + "- like 'Product = WidgetXYZ' (or similar).\r\n +" +
        "-" + nameof(LeftmostColumn) + "- is set correspondingly.")]
    public class FieldIterator : BaseEntity {

        [ClassMember(Description =
            "The actual values to show. " +
            "Note that for a given collection of -" + nameof(FieldIterator) + "-, all -" + nameof(Columns) + "--collections should have the same number of elements in the same order"
        )]
        private List<Property> Columns; //  { get; private set; }

        [ClassMember(Description = "Something like Sum, Median or 'Product = WidgetXYZ'")]
        private string LeftmostColumn; // { get; private set; }

        public FieldIterator(AggregationType rowKey, List<Property> columns) {
            LeftmostColumn = rowKey.ToString();
            Columns = columns;
        }

        public FieldIterator(DrillDownSuggestion drillDownSuggestion, List<Property> columns) {
            LeftmostColumn = drillDownSuggestion.Text;
            Columns = columns;
        }

        public override List<PropertyKey> ToHTMLTableColumns(Request request) => Columns.Select(p => (PropertyKey)p.Key).ToList();

        public override string ToHTMLTableRowHeading(Request request, List<AggregationType> aggregateRows) {
            if (aggregateRows != null) throw new NotNullReferenceException(nameof(aggregateRows));
            return "<tr><th>&nbsp;</th>" + string.Join("", Columns.Select(p => "<th>" + p.Key.ToHTMLTableHeader() + "</th>")) + "</tr>";
        }

        public override string ToHTMLTableRow(Request request) => "<tr><td>" +
            LeftmostColumn.HTMLEncode() + "</td>" + string.Join("", Columns.Select(p => "<td>" + 
                p.V<Property.HTML>() + "</td>"));

        public override string ToHTMLDetailed(Request request) => throw new NotImplementedException("Would be rather meaningless to implement anyway.");

        public override List<PropertyKey> ToCSVTableColumns(Request request) => throw new NotImplementedException();
        public override string ToCSVTableRowHeading(Request request) => throw new NotImplementedException();
        public override string ToCSVTableRow(Request request) => throw new NotImplementedException();
        public override string ToCSVDetailed(Request request) => throw new NotImplementedException("Would be rather meaningless to implement anyway.");

    }
}