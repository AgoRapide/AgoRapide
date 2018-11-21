// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;

namespace AgoRapide.API {

    /// <summary>
    /// NOTE: Never stored in database.
    /// </summary>
    [Class(Description =
        "Used by " + nameof(BaseController.AgoRapideGenericMethod) + " " +
        "in order to present possible methods that the client intended to call.")]
    public class APIMethodCandidate : BaseEntity {

        public APIMethod Method { get; private set; }
        public int LastMatchingSegmentNo { get; private set; }
        public RouteSegmentClass FirstNonMatchingSegment { get; private set; }

        // public Uri SuggestedUrl => PV<Uri>(CoreP.SuggestedUrl.A());

        /// <summary>
        /// </summary>
        /// <param name="method"></param>
        /// <param name="lastMatchingSegmentNo">Counts from 0. May be -1</param>
        public APIMethodCandidate(Request request, APIMethod method, int lastMatchingSegmentNo) {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            LastMatchingSegmentNo = (lastMatchingSegmentNo >= -1 && lastMatchingSegmentNo < (method.RouteSegments.Count - 1)) ? lastMatchingSegmentNo : throw new ArgumentException(nameof(lastMatchingSegmentNo) + " (" + lastMatchingSegmentNo + ") does not fall within valid index value minus one for " + nameof(method) + "." + nameof(method.RouteSegments) + " (which has .Count = " + method.RouteSegments.Count + "), nor is it -1");
            FirstNonMatchingSegment = Method.RouteSegments[LastMatchingSegmentNo + 1];
            AddProperty(CoreP.SuggestedUrl.A(), new Uri(request.JSONUrl + "/" + FirstNonMatchingSegment.SampleValues[0] + (request.ResponseFormat == ResponseFormat.HTML ? Util.Configuration.C.HTMLPostfixIndicator : (request.ResponseFormat == ResponseFormat.CSV ? Util.Configuration.C.CSVPostfixIndicator : ""))));
            AddProperty(CoreP.IdFriendly.A(), Method.IdFriendly); /// Alternative to overriding <see cref="BaseEntity.IdFriendly"/>
        }
    }
}
