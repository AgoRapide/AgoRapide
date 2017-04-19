using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.Core {

    [Class( 
        Description = 
            "The simplest form of -" + nameof(QueryId) + "-, " +
            "accepting only integer id's " +
            "corresponding to -" + nameof(DBField.id) + "-",
        SampleValues = new string[] { "42" })]
    public class QueryIdInteger : QueryId {
        public long Id { get; private set; }
        public override string ToString() => Id.ToString();

        public QueryIdInteger(long id) {
            Id = id != 0 ? id : throw new ArgumentException(nameof(id) + ": " + id);
            IsSingle = true;
            IsMultiple = false;
            SQLWhereStatement = "WHERE " + DBField.id + " = " + Id;
        }

        /// <summary>
        /// TODO: USE ONE COMMON GENERIC METHOD FOR EnrichAttribute for all QueryId-classes!!!
        /// TODO: IMPLEMENT CLEANER AND CHAINING OF CLEANER
        /// TODO: enumAttribute.Cleaner=
        /// TODO: IMPLEMENT CHAINING OF VALIDATION!
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        public new static void EnrichAttribute(PropertyKeyAttributeEnriched agoRapideAttribute) =>
            agoRapideAttribute.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(value, out var retval, out var errorResponse) ?
                    (retval is QueryIdInteger ? /// <see cref="QueryId.TryParse"/> returns <see cref="QueryId"/> only accept if <see cref="QueryIdInteger"/>
                    ParseResult.Create(agoRapideAttribute, retval) :
                        ParseResult.Create("Not a valid " + typeof(QueryIdInteger).ToStringShort() + " (found " + retval.GetType().ToStringShort() + ")")
                        ) :
                        ParseResult.Create(errorResponse);
            });
    }
}
