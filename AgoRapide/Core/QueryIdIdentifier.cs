using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;

namespace AgoRapide.Core {

    [PropertyKey(
        Description =
            "Lookup based on -" + nameof(CoreP.Identifier) + "-. " +
            "Identical to -" + nameof(QueryIdKeyOperatorValue) + "- WHERE " + nameof(CoreP.Identifier) + " = {value}",
        SampleValues = new string[] { "Property/{QueryId}" } /// The sample values identifies a specific <see cref="APIMethod"/>
    )]
    class QueryIdIdentifier : QueryIdKeyOperatorValue {
        public QueryIdIdentifier(string value) : base(CoreP.Identifier.A().Key, Operator.EQ, value) {
            CoreP.Identifier.A().Key.A.AssertIsUniqueInDatabase(); /// Assert since the whole concept of this class assumes <see cref="PropertyKeyAttribute.IsUniqueInDatabase"/>
            _toString = value; /// Improve on use of <see cref="QueryId.ToString"/>
        }
    }
}
