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
    [AgoRapide(
        Description = "Communicates result of -" + nameof(CoreMethod.GeneralQuery) + "-",
        AccessLevelRead = AccessLevel.User // For JSON to work something must be specified here
    )]
    public class GeneralQueryResult : BaseEntityT {

        public override string ToHTMLTableHeading(Request request) => "<tr><th>Result</th></tr>";

        public override string ToHTMLTableRow(Request request) => "<tr><td>" +
            "<a href=\"" + PV<string>(CoreP.SuggestedUrl.A()) + "\">" + PV<string>(CoreP.Description.A()).HTMLEncode() + "</a>" +
            "</tr>\r\n";

    }
}
