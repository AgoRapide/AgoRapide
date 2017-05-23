// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
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
            "Corresponds to -" + nameof(CoreP.QueryId) + "- and -" + nameof(QueryIdString) + "-.\r\n" +
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

        /// <summary>
        /// May be null. 
        /// </summary>
        [ClassMember(Description =
            "Corresponds to -" + nameof(CoreP.QueryIdParent) + "-.\r\n" +
            "See -" + nameof(CoreP.QueryIdParent) + "- for documentation.")]
        public QueryId Parent { get; private set; }

        public Id(QueryIdString idString, string idFriendly, List<string> idDoc) : this(idString, idFriendly, idDoc, null) { }
        /// <summary>
        /// </summary>
        /// <param name="idString"></param>
        /// <param name="idFriendly"></param>
        /// <param name="idDoc"></param>
        /// <param name="parent">May be null</param>
        public Id(QueryIdString idString, string idFriendly, List<string> idDoc, QueryId parent) {
            IdString = idString ?? throw new NullReferenceException(nameof(idString));
            // InvalidIdentifierException.AssertValidIdentifier(IdString);
            IdFriendly = idFriendly ?? throw new NullReferenceException(nameof(idFriendly));
            IdDoc = idDoc ?? throw new NullReferenceException(nameof(idDoc));
            Parent = parent; // May be null
        }
    }
}