// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {

    /// <summary>
    /// NOTE: Never stored in database. 
    /// </summary>
    [Class(
        Description = "Communicates result of -" + nameof(CoreAPIMethod.GeneralQuery) + "-",
        LongDescription = "Usually contains a -" + nameof(CoreP.SuggestedUrl) + "- and -" + nameof(CoreP.Description) + "-.",
        AccessLevelRead = AccessLevel.User
    )]
    public class GeneralQueryResult : BaseEntity {

        /// <summary>
        /// Consider removing <paramref name="request"/> from <see cref="BaseEntity.ToHTMLTableRowHeading"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public override string ToHTMLTableRowHeading(Request request) => HTMLTableHeading;
        public const string HTMLTableHeading = "<tr><th>Result</th></tr>";

        public override string ToHTMLTableRow(Request request) => "<tr><td>" +
            "<a href=\"" + PV<string>(CoreP.SuggestedUrl.A()) + "\">" + PV<string>(CoreP.Description.A()).HTMLEncode() + "</a>" +
            "</tr>\r\n";

    }
}
