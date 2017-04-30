using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;

namespace AgoRapide.Core {

    [Class(
        Description =
            "Lookup based on -" + nameof(CoreP.IdString) + "-. " +
            "Identical to -" + nameof(QueryIdKeyOperatorValue) + "- WHERE " + nameof(CoreP.IdString) + " = {value}",
        LongDescription =
            "Corresponds to -" + nameof(CoreP.IdString) + "- and -" + nameof(Id.IdString) + "-.",
        SampleValues = new string[] { "APIMethod_Property__QueryId_" } /// The sample values identifies a specific <see cref="APIMethod"/>
    )]
    public class QueryIdString : QueryIdKeyOperatorValue {
        public QueryIdString(string value) : base(CoreP.IdString.A().Key, Operator.EQ, value) {
            CoreP.IdString.A().Key.A.AssertIsUniqueInDatabase();
            _toString = value; /// Improve on use of <see cref="QueryId.ToString"/>
        }
    }
}
