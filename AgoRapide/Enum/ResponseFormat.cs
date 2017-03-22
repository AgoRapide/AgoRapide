using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;

namespace AgoRapide {

    /// <summary>
    /// The response format requested by the client. 
    /// JSON is default, HTML will be returned when request URL ends with 
    /// <see cref="Configuration.HTMLPostfixIndicator"/> ("HTML")
    /// 
    /// Note how XML can be disabled and JSON added 
    /// like this in Startup.cs class WebApiConfig static method Register(System.Web.Http.HttpConfiguration httpConfiguration):
    ///    httpConfiguration.Formatters.Remove(httpConfiguration.Formatters.XmlFormatter);
    ///    httpConfiguration.Formatters.JsonFormatter.MediaTypeMappings.Add(new System.Net.Http.Formatting.QueryStringMapping("json", "true", "application/json"));
    /// </summary>
    public enum ResponseFormat {
        None,

        /// <summary>
        /// Will be used when the request URL does not end with <see cref="Configuration.HTMLPostfixIndicator"/> ("/HTML")
        /// 
        /// See also <see cref="JSONView{TProperty}"/>,  <see cref="BaseEntityT{TProperty}.ToJSONEntity"/>
        /// </summary>
        JSON,

        /// <summary>
        /// Will be used then the request URL ends with <see cref="Configuration.HTMLPostfixIndicator"/> ("/HTML")
        /// 
        /// See also <see cref="HTMLView{TProperty}"/>, <see cref="BaseEntityT{TProperty}.ToHTMLTableHeading"/>, <see cref="BaseEntityT{TProperty}.ToHTMLTableRow"/>, <see cref="BaseEntityT{TProperty}.ToHTMLDetailed"/>
        /// </summary>
        HTML,
    }
}