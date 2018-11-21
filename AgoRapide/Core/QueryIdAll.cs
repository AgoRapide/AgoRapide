// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    [Class(Description = "Represents all entities (-" + nameof(IsMatch) + "- returns TRUE for all entities)")]
    public class QueryIdAll : QueryId {
        public QueryIdAll() {
            _SQLWhereStatement = null; // Note 
        }
        public override bool IsMatch(BaseEntity entity) => true;
        public override string ToString() => "All";
    }
}