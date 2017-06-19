using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    [Class(Description = "Signifies that " + nameof(CoreP.Context) + " (" + nameof(Context) + ") for the current user is to be queried")]
    public class QueryIdContext : QueryId {

        [ClassMember(Description = "See corresponding code in -" + nameof(QueryId.TryParse) + "-.")]

        public const string AS_STRING = "CurrentContext";
        public const string AS_STRING_ToLower = "currentcontext";

        public override string ToString() => AS_STRING;
    }
}
