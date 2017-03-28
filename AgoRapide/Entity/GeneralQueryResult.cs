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
    [AgoRapide(Description = "Communicates result of -" + nameof(CoreMethod.GeneralQuery) + "-")]
    public class GeneralQueryResult : BaseEntityT {

        public override string ToHTMLTableHeading(Request request) => "<tr><th>Result</th></tr>";

        public override string ToHTMLTableRow(Request request) => "<tr><td>" +
            "<a href=\"" + PV<string>(CoreProperty.SuggestedUrl) + "\">" + PV<string>(CoreProperty.Description).HTMLEncode() + "</a>" +
            "</tr>\r\n";

    }
}
