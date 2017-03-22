using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;

namespace AgoRapide {

    /// <summary>
    /// Never stored in database. 
    /// </summary>
    /// <typeparam name="TProperty"></typeparam>
    [AgoRapide(Description = "Communicates result of -" + nameof(CoreMethod.GeneralQuery) + "-")]
    public class GeneralQueryResult<TProperty> : BaseEntityT<TProperty> where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"

        public override string ToHTMLTableHeading(Request<TProperty> request) => "<tr><th>Result</th></tr>";

        public override string ToHTMLTableRow(Request<TProperty> request) => "<tr><td>" +
            "<a href=\"" + PV<string>(M(CoreProperty.SuggestedUrl)) + "\">" + PV<string>(M(CoreProperty.Description)).HTMLEncode() + "</a>" +
            "</tr>\r\n";

    }
}
