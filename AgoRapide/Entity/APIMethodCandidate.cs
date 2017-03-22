﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;

namespace AgoRapide {

    /// <summary>
    /// Never stored in database.
    /// </summary>
    /// <typeparam name="TProperty"></typeparam>
    [AgoRapide(Description = 
        "Used by " + nameof(BaseController<CoreProperty>.AgoRapideGenericMethod) +" " +
        "in order to present possible methods that the client intended to call.")]
    public class APIMethodCandidate<TProperty> : BaseEntityT<TProperty> where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"

        public APIMethod<TProperty> Method { get; private set; }
        public int LastMatchingSegmentNo { get; private set; }
        public RouteSegmentClass<TProperty> FirstNonMatchingSegment { get; private set; }

        public string SuggestedUrl => PV<string>(M(CoreProperty.SuggestedUrl));

        public override string Name => Method.Name;

        /// <summary>
        /// </summary>
        /// <param name="method"></param>
        /// <param name="lastMatchingSegmentNo">Counts from 0. May be -1</param>
        public APIMethodCandidate(Request<TProperty> request, APIMethod<TProperty> method, int lastMatchingSegmentNo) {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            LastMatchingSegmentNo = (lastMatchingSegmentNo >= -1 && lastMatchingSegmentNo < (method.RouteSegments.Count - 1)) ? lastMatchingSegmentNo : throw new ArgumentException(nameof(lastMatchingSegmentNo) + " (" + lastMatchingSegmentNo + ") does not fall within valid index value minus one for " + nameof(method) + "." + nameof(method.RouteSegments) + " (which has .Count = " + method.RouteSegments.Count + "), nor is it -1");
            FirstNonMatchingSegment = Method.RouteSegments[LastMatchingSegmentNo + 1];
            AddProperty(M(CoreProperty.SuggestedUrl), request.JSONUrl + "/" + FirstNonMatchingSegment.SampleValues[0] + (request.ResponseFormat==ResponseFormat.HTML ? Util.Configuration.HTMLPostfixIndicator  : ""));
        }
    }
}
