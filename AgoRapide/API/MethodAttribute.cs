﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {

    /// <summary>
    /// See slightly more refined class <see cref="MethodAttributeT"/>
    /// 
    /// TODO: Add some kind of SuggestedNextMethod / SuggestedNextCommand, that will be automatically added in HTML-responses
    /// TODO: to client. We have a problem of mapping them though, but autogenerated methods could contain such a thing. 
    /// </summary>
    [AgoRapide(Description = "General attributes for an API-method.")]
    public class MethodAttribute : Attribute {

        public CoreMethod CoreMethod { get; set; }
        public void AssertCoreMethod(CoreMethod coreMethod) {
            if (CoreMethod != coreMethod) throw new InvalidEnumException(CoreMethod, "Expected " + coreMethod);
        }

        /// <summary>
        /// TODO: Implement support for <see cref="MethodAttribute.RouteTemplate"/>.
        /// </summary>
        public string RouteTemplate { get; set; }

        [AgoRapide(Description = "Route segment 1. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S1 { get; set; }
        [AgoRapide(Description = "Route segment 2. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S2 { get; set; }
        [AgoRapide(Description = "Route segment 3. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S3 { get; set; }
        [AgoRapide(Description = "Route segment 4. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S4 { get; set; }
        [AgoRapide(Description = "Route segment 5. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S5 { get; set; }
        [AgoRapide(Description = "Route segment 6. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S6 { get; set; }
        [AgoRapide(Description = "Route segment 7. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S7 { get; set; }
        [AgoRapide(Description = "Route segment 8. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S8 { get; set; }
        [AgoRapide(Description = "Route segment 9. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S9 { get; set; }

        /// <summary>
        /// <see cref="Request.CurrentUser"/> (<see cref="BaseEntity.AccessLevelGiven"/>) must by equal to <see cref="MethodAttribute.AccessLevelUse"/> or HIGHER in order for access to be granted to <see cref="APIMethod"/>
        /// </summary>
        public AccessLevel AccessLevelUse { get; set; } = AccessLevel.User;

        /// <summary>
        /// TRUE means that detailed result information like <see cref="Result.LogData"/> and <see cref="Result.Counts"/> 
        /// shall be returned to client. This is useful for instance for long duration complex methods where the (human) client usually wants to 
        /// understand what is going on internally in the API.
        /// </summary>
        public bool ShowDetailedResult { get; set; } = false;

        /// <summary>
        /// The current <see cref="Util.Configuration"/>.<see cref="Configuration.Environment"/> has to be equivalent or lower in order for the method to be included in the API routing
        /// </summary>
        public Environment Environment { get; set; } = Environment.Production;

        /// <summary>
        /// Note that you may set Description through either [Description("...")] or [AgoRapideAttribute(Description = "...")]. The last one takes precedence. 
        /// </summary>
        public string Description { get; set; }

        public string LongDescription { get; set; }

        /// <summary>
        /// TODO: Not inplemented as of Jan 2017
        /// 
        /// Next method that client is suggested to call. 
        /// Typical would be to suggest Person/{id} after doing Person/Add/{first_name}/{last_name} for instance. 
        /// <see cref="Result.ToHTMLDetailed"/> could add this, since it knows request, parameters and so on.
        /// Typical would be to replace {id} in this string with result values like <see cref="CoreP.Id"/>. 
        /// We could also add this to <see cref="Result.ToJSONDetailed"/>
        /// </summary>
        public string SuggestedNextMethod { get; set; }
    }
}
