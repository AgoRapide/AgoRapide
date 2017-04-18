using System.ComponentModel;
using AgoRapide.Core;
using AgoRapide.API;
using AgoRapide.Database;

namespace AgoRapide {

    [Enum(
        Description = "Describes in detail how an API operation failed.",
        LongDescription = "Usually contained as -" + nameof(CoreP.ResultCode) + "- within -" + nameof(Result) + "-. Ordering should be in increasing order of \"seriousness\".",
        EnumTypeY = EnumType.DataEnum)]
    public enum ResultCode {
        None,

        ok,

        [EnumMember(Description = "Query was not syntactically correct.\r\nMistake is assumed to reside with the client.")]
        client_error,

        [EnumMember(
            Description = "Access was denied.",
            LongDescription = "Occurs seldom or never because access restrictions are usually implemented at HTTP-level with Basic Authorization, OAuth or similar."
        )]
        access_error,

        /// <summary>
        /// TODO: Define name of suggested value
        /// </summary>
        [EnumMember(Description = "An obligatory parameter or part of URL necessary to identify method was missing.\r\nIncluded in the response will be a suggested value (if defined as -" + nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.SampleValues) + "- or found by -RouteSegmentClass-)")]
        missing_parameter_error,

        /// <summary>
        /// TODO: Define name of suggested value
        /// </summary>
        [EnumMember(Description = "A parameter had an invalid value. Included in the response will be a suggested value (if defined as " + nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.DefaultValue) + ")")]
        invalid_parameter_error,

        [EnumMember(Description = "Corresponding data was not found in the database or existing data was inconsistent with query")]
        data_error,

        [EnumMember(Description = "Some kind of problems communicating with underlying systems (database or similar)")]
        communication_error,

        [EnumMember(Description = "Internal AgoRapide or REST API application error.\r\nThese errors should never occur.\r\nIf they occur them some corrections should be made to either the AgoRapide library or to the functionality of the REST API application that is being called. Therefore you should contact your IT-administrator / support department if you get these kind of errors.")]
        exception_error
    }
}
