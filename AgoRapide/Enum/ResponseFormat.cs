using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// Note how XML can be disabled and JSON added 
    /// like this in Startup.cs class WebApiConfig static method Register(System.Web.Http.HttpConfiguration httpConfiguration):
    ///    httpConfiguration.Formatters.Remove(httpConfiguration.Formatters.XmlFormatter);
    ///    httpConfiguration.Formatters.JsonFormatter.MediaTypeMappings.Add(new System.Net.Http.Formatting.QueryStringMapping("json", "true", "application/json"));
    /// </summary>
    [AgoRapide(
        Description = 
            "The response format requested by the client. " +
            "JSON is default, HTML will be returned when request URL ends with -" + nameof(Configuration.HTMLPostfixIndicator) + "- (\"HTML\")",
        EnumType = EnumType.DataEnum)]
    public enum ResponseFormat {
        None,

        /// <summary>
        /// Will be used when the request URL does not end with <see cref="Configuration.HTMLPostfixIndicator"/> ("/HTML")
        /// 
        /// See also <see cref="JSONView"/>,  <see cref="BaseEntityT.ToJSONEntity"/>
        /// </summary>
        JSON,

        /// <summary>
        /// Will be used then the request URL ends with <see cref="Configuration.HTMLPostfixIndicator"/> ("/HTML")
        /// 
        /// See also <see cref="HTMLView"/>, <see cref="BaseEntityT.ToHTMLTableHeading"/>, <see cref="BaseEntityT.ToHTMLTableRow"/>, <see cref="BaseEntityT.ToHTMLDetailed"/>
        /// </summary>
        HTML,
    }
}