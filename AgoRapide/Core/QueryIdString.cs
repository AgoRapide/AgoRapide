// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;
using AgoRapide.API;

namespace AgoRapide.Core {

    /// <summary>
    /// Note how <see cref="QueryId.TryParse"/> can recognize <see cref="CoreP.IdDoc"/> and replace with the full <see cref="CoreP.QueryId"/>
    /// 
    /// Note how literal string "<see cref="CoreAPIMethod.Context"/>" is treated by <see cref="BaseController.HandleCoreMethodEntityIndex"/>
    /// </summary>
    [Class(
        Description =
            "Human friendly lookup based on -" + nameof(CoreP.QueryId) + "-.\r\n" +
            "Identical to -" + nameof(QueryIdKeyOperatorValue) + "- WHERE " + nameof(CoreP.QueryId) + " = {value}\r\n" +
            "Note how -" + nameof(QueryIdString) + "- will often be the same among different database instances in contrast to -" + nameof(QueryIdInteger) + "- " +
            "(this especially applies to -" + nameof(ApplicationPart) + "- and reduces prevalence of link-rot in documentation) .",
        LongDescription =
            "Will often correspond to -" + nameof(CoreP.QueryId) + "- (from -" + nameof(Id.IdString) + "-)." +
            "Values chosen should be compatible with HTTP GET URLs (without any escaping of characters). " +
            "The approach chosen is therefore to assure that values -" + nameof(PropertyKeyAttribute.MustBeValidCSharpIdentifier) + "-.",
        SampleValues = new string[] { "APIMethod_Property__QueryId_" } /// The sample values identifies a specific <see cref="APIMethod"/>
    )]
    public class QueryIdString : QueryIdKeyOperatorValue {
        public QueryIdString(string value) : base(CoreP.QueryId.A().Key, Operator.EQ, value) {
            CoreP.QueryId.A().Key.A.AssertIsUniqueInDatabase();
            InvalidIdentifierException.AssertValidIdentifier(value);
            _toString = value; /// TODO: Improve on use of <see cref="QueryId.ToString"/>
        }
    }
}
