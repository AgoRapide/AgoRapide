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

        /// TODO: To be deleted. 
        /// TODO: Wrong assumption. This should probably be added by "normal" mechanism for presenting results.
        //public FieldIterator(AggregationType rowKey, List<Property> columns) {
        //    LeftmostColumn = rowKey.ToString();
        //    Columns = columns;
        //}

        /// TODO: To be deleted. 
        /// TODO: Wrong assumption since there are different <see cref="DrillDownSuggestion"/> for each column
        //public FieldIterator(DrillDownSuggestion drillDownSuggestion, List<Property> columns) {
        //    LeftmostColumn = drillDownSuggestion.Text;
        //    Columns = columns;
        //}

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
            ///// TODO: Why "deny" use of <param name="aggregateRows"/> here?
            //if (aggregateRows != null && aggregateRows.Count > 0) throw new NotNullReferenceException(nameof(aggregateRows));
            aggregateRows = aggregateRows?.Where(a => ToHTMLTableColumns(request).Where(t => t.Key.A.AggregationTypes.Any(ka => ka == a)).Count() > 0).ToList() ?? null;

            return "<thead>" +
                (aggregateRows == null || aggregateRows.Count == 0 ? "" : (
                    headers + // Note how field names (headers) are repeated multiple times. TODO: Make better when very few aggregations / contexts suggestions.
                    string.Join("", aggregateRows.Select(a => {
                        // if (ToHTMLTableColumns(request).Where(t => t.Key.A.AggregationTypes.Any(ka => ka == a)).Count() == 0) return "";
                        return "<tr><th>" + a + "</th>" +
                            string.Join("", ToHTMLTableColumns(request).Select(key => "<th align=\"right\">" +
                                (key.Key.A.AggregationTypes.Contains(a) ? ("<!--" + key.Key.PToString + "_" + a + "-->") : "") +
                                "</th>")) +
                        "</tr>";
                    })) // +
                        // headers.Replace(">" + nameof(IdFriendly) + "<",">&nbsp;<") // Note how field names (headers) are repeated multiple times. TODO: Make better when very few aggregations / contexts suggestions.
                )) +
                // Removed 29 Nov 2017
                //"<tr><th>Context</th>" +
                //    string.Join("", ToHTMLTableColumns(request).Select(key => "<th  style=\"vertical-align:top\"><!--" + key.Key.PToString + "_Context--></th>")) +
                //"</tr>" +
                headers + // Note how field names (headers) are repeated multiple times. TODO: Make better when very few aggregations / contexts suggestions.
                "</thead>";
        }

        public override string ToHTMLTableRow(Request request) => "<tr><td>" +
            LeftmostColumn.HTMLEncode() + "</td>" + string.Join("", Columns.Select(p => "<td" + (p.Key.Key.A.NumberFormat == NumberFormat.None ? "" : " align=\"right\"") + ">" +
                p.V<Property.HTML>() + "</td>"));

        public override string ToHTMLDetailed(Request request) => throw new NotImplementedException("Would be rather meaningless to implement anyway.");
    
        public override List<PropertyKey> ToCSVTableColumns(Request request) => ToHTMLTableColumns(request);
        // public override string ToCSVTableRowHeading(Request request) => throw new NotImplementedException(); // There is no need for overriding this
        public override string ToCSVTableRow(Request request) =>
            LeftmostColumn + request.CSVFieldSeparator + string.Join(request.CSVFieldSeparator, Columns.Select(p =>
                p.V<string>().Replace(request.CSVFieldSeparator, ":").Replace("\r\n", " // ") // Note replacement here with : and //. TODO: Document better / create alternatives
            )) + "\r\n";

        public override string ToCSVDetailed(Request request) => throw new NotImplementedException("Would be rather meaningless to implement anyway.");
    }

    // TODO: Decide if this is needed? 
    // TODO: Maybe keys have to be generated dynamically anyway?
    //[Enum(AgoRapideEnumType = EnumType.PropertyKey)]
    //public enum FieldIteratorP {

    //    None,

    //    [PropertyKey(Type = typeof(long), Group = typeof(FieldIteratorPropertiesDescriber))]
    //    Value,
    //}

    //public static class ExtensionsFieldIteratorP {
    //    public static PropertyKey A(this FieldIteratorP p) => PropertyKeyMapper.GetA(p);
    //}

    //public class FieldIteratorPropertiesDescriber : IGroupDescriber {
    //    public void EnrichKey(PropertyKeyAttributeEnriched key) {
    //        key.AddParent(typeof(FieldIterator));
    //        key.A.AccessLevelRead = AccessLevel.Relation; 
    //        key.A.AccessLevelWrite = AccessLevel.Relation;
    //    }
    //}
}