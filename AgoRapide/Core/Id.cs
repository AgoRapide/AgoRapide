using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;

namespace AgoRapide.Core {

    [Class(Description =
        "Practical class containing different ways of representing id's.\r\n" +
        "Mostly used for -" + nameof(ApplicationPart) + "- (or rather -" + nameof(BaseAttribute) + "-).\r\n" +
        "Note that does not contain -" + nameof(DBField.id) + "- / - " + nameof(BaseEntity.Id) + "-.")]
    public class Id {

        [ClassMember(Description = 
            "Corresponds to -" + nameof(CoreP.QueryId) + "- and -" + nameof(QueryIdString) + "-.\r\n"+
            "See -" + nameof(CoreP.QueryId) + "- for documentation.")]
        public QueryIdString IdString { get; private set; }

        [ClassMember(Description =
            "Corresponds to -" + nameof(CoreP.IdFriendly) + "-.\r\n" +
            "See -" + nameof(CoreP.IdFriendly) + "- for documentation.")]
        public string IdFriendly { get; private set; }

        [ClassMember(Description =
            "Corresponds to -" + nameof(CoreP.IdDoc) + "-.\r\n" +
            "See -" + nameof(CoreP.IdDoc) + "- for documentation.")]
        public List<string> IdDoc { get; private set; }

        public Id(QueryIdString idString, string idFriendly, List<string> idDoc) {
            IdString = idString ?? throw new NullReferenceException(nameof(idString));
            // InvalidIdentifierException.AssertValidIdentifier(IdString);
            IdFriendly = idFriendly ?? throw new NullReferenceException(nameof(idFriendly));
            IdDoc = idDoc ?? throw new NullReferenceException(nameof(idDoc));
        }
    }
}