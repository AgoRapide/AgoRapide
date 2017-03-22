﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// TODO: Add support for <see cref="Template"/> and also support for attribute routing in .NET.
    /// </summary>
    [AgoRapide(
        Description = "Describes how a given API-method was constructed.",
        LongDescription =
            "-" + nameof(Autogenerated) + "- and -" + nameof(SemiAutogenerated) + "- has the advantage of " +
            "building your API in a standardised manner with validation of input parameters, documentation, samples for the user and unit tests."
        )]
    public enum APIMethodOrigin {
        None,

        /// <summary>
        /// Note how AgoRapideSample.BasicAuthenticationAttribute gives an example about how to accept anonymous access to {*url} for
        /// <see cref="Autogenerated"/> methods without <see cref="CoreProperty.RequiresAuthorization"/> (and also accepts anonymous access for
        /// <see cref="SemiAutogenerated"/> methods for which there are supposed to be missing parameters). 
        /// </summary>
        [AgoRapide(
            Description = "Autogenerated by -" + nameof(APIMethod<CoreProperty>) + "." + nameof(APIMethod<CoreProperty>.CreateAutogeneratedMethods) + "-",
            LongDescription =
                "These methods are not used by -" + nameof(APIMethodMapper<CoreProperty>) + "- (that is, they are not given to the standard " +
                "ASP .NET mapping mechanism). Instead they are used by -" + nameof(BaseController<CoreProperty>.AgoRapideGenericMethod) + "- " +
                "(which handles {*url} in order to recognize which internal method to call. " +
                "\r\n" +
                "The advantage here is that no manual coding is involved. Lots of \"standard\" API-operations may usually be routed in this manner. ")]
        Autogenerated,

        [AgoRapide(
            Description = "Semi-autogenerated by -" + nameof(APIMethod<CoreProperty>) + "." + nameof(APIMethod<CoreProperty>.CreateSemiAutogeneratedMethods) + "-",
            LongDescription =
                "This is done by using the -" + nameof(MethodAttribute) + "--class in order to tag the Controller's action (method) with " +
                "the different -" + nameof(RouteSegmentClass<CoreProperty>) + "--classes " +
                "(-" + nameof(MethodAttribute.S1) + "-, -" + nameof(MethodAttribute.S2) + "- and so on) " +
                "that describes the " + nameof(APIMethod<CoreProperty>.RouteTemplates) + "-. ")]
        SemiAutogenerated,

        [AgoRapide(
            Description = "Traditional WebAPI template. ",
            LongDescription =
                "Typical example: Person/Add/{first_name}/{last_name}\r\n" +
                "Not supported as of Feb 2017")]
        Template
    }
}