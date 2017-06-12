// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
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
        // public override string ToAPIQuery() => ToString();

        public QueryIdInteger(long id) {
            Id = id != 0 ? id : throw new ArgumentException(nameof(id) + ": " + id);
            IsSingle = true;
            IsMultiple = false;
            _SQLWhereStatement = "WHERE " + DBField.id + " = " + Id;
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
                    (retval is QueryIdInteger ? /// Note how <see cref="QueryId.TryParse"/> returns base class <see cref="QueryId"/>, therefore only accept the returned value if it is a <see cref="QueryIdInteger"/>
                        ParseResult.Create(agoRapideAttribute, retval) :
                        ParseResult.Create("Not a valid " + typeof(QueryIdInteger).ToStringShort() + " (found " + retval.GetType().ToStringShort() + ")")
                    ) :
                    ParseResult.Create(errorResponse);
            });
    }
}
