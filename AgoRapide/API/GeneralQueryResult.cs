// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {

    /// <summary>
    /// NOTE: Never stored in database. 
    /// </summary>
    [Class(
        Description = 
            "Communicates result of -" + nameof(CoreAPIMethod.GeneralQuery) + "-. " +
            "Also used for general communication of API call suggestions.",
        LongDescription = 
            "Usually contains a -" + nameof(CoreP.SuggestedUrl) + "- and -" + nameof(CoreP.Description) + "-.",
        AccessLevelRead = AccessLevel.User
    )]
    public class GeneralQueryResult : BaseEntity {

        public GeneralQueryResult() {
        }

        /// <summary>
        /// Convenience constructor.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="description"></param>
        public GeneralQueryResult(Uri url, string description) {
            AddProperty(CoreP.SuggestedUrl.A(), url);
            AddProperty(CoreP.Description.A(), description);
        }

        private static ConcurrentDictionary<
            string, // Key is GetType + _ + PriorityOrderLimit
            List<PropertyKey>> _tableRowColumnsCache = new ConcurrentDictionary<string, List<PropertyKey>>();
        public override List<PropertyKey> ToHTMLTableColumns(Request request) => _tableRowColumnsCache.GetOrAdd(GetType() + "_" + request.PriorityOrderLimit, k => new List<PropertyKey> { CoreP.SuggestedUrl.A() } );

        ///// <summary>
        ///// NOTE: In principle this override is unnecessary as <see cref="ToHTMLTableColumns"/> communicates what is needed.
        ///// </summary>
        ///// <param name="request"></param>
        ///// <returns></returns>
        //public override string ToHTMLTableRowHeading(Request request) => HTMLTableHeading;
        //public const string HTMLTableHeading = "<tr><th>Result</th></tr>";

        public override string ToHTMLTableRow(Request request) => "<tr><td>" +
            "<a href=\"" + PV<Uri>(CoreP.SuggestedUrl.A()) + "\">" + PV<string>(CoreP.Description.A()).HTMLEncode() + "</a>" +
            "</tr>\r\n";

    }
}
