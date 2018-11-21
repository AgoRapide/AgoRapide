// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
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
    /// like this in your Startup.cs class WebApiConfig static method Register(System.Web.Http.HttpConfiguration httpConfiguration):
    ///    httpConfiguration.Formatters.Remove(httpConfiguration.Formatters.XmlFormatter);
    ///    httpConfiguration.Formatters.JsonFormatter.MediaTypeMappings.Add(new System.Net.Http.Formatting.QueryStringMapping("json", "true", "application/json"));
    /// </summary>
    [Enum(
        Description =
            "The response format requested by the client. " +
            "JSON is default, " +
            "HTML will be returned when request URL ends with -" + nameof(ConfigurationAttribute.HTMLPostfixIndicator) + "- (\"HTML\"), " +
            "CSV will be returned when request URL ends with -" + nameof(ConfigurationAttribute.CSVPostfixIndicator) + "- (\"CSV\").",
        AgoRapideEnumType = EnumType.EnumValue)]
    public enum ResponseFormat {
        None,

        /// <summary>
        /// See also <see cref="JSONView"/>, <see cref="BaseEntity.ToJSONEntity"/>
        /// </summary>
        [EnumValue(Description = 
            "For machine consumption, typical by a smartphone app.\r\n" +
            "Will be used when the request URL does not end with neither -" + nameof(ConfigurationAttribute.HTMLPostfixIndicator) + "- (\"HTML\") nor -" + nameof(ConfigurationAttribute.CSVPostfixIndicator) + "- (\"CSV\").")]
        JSON,

        /// <summary>
        /// See also <see cref="HTMLView"/>, <see cref="BaseEntity.ToHTMLTableRowHeading"/>, <see cref="BaseEntity.ToHTMLTableRow"/>, <see cref="BaseEntity.ToHTMLDetailed"/>
        /// </summary>
        [EnumValue(Description = 
            "For human online consumption, note how -" + nameof(HTMLView) + "- inserts lots of useful links and documentation in the returned data.\r\n" +
            "Will be used when the request URL ends with -" + nameof(ConfigurationAttribute.HTMLPostfixIndicator) + "-. (\"HTML\").")]
        HTML,

        /// <summary>
        /// See also <see cref="PDFView"/>
        /// </summary>
        [EnumValue(Description =
            "For human offline consumption / paper format..\r\n" +
            "Will be used when the request URL ends with -" + nameof(ConfigurationAttribute.PDFPostfixIndicator) + "-. (\"PDF\").")]
        PDF,

        /// <summary>
        /// See also <see cref="CSVView"/>
        /// </summary>
        [EnumValue(Description = 
            "For export into other systems, typical a spreadsheet program like Libre Office Calc or Microsoft Excel.\r\n" +
            "Will be used when the request URL ends with -" + nameof(ConfigurationAttribute.CSVPostfixIndicator) + "-. (\"CSV\").")]
        CSV,
    }
}