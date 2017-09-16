// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapide.API {

    [Class(Description = "Generates -" + nameof(ResponseFormat.HTML) + "--view of results.")]
    public class HTMLView : BaseView {
        public HTMLView(Request request) : base(request) { }
        /// <summary>
        /// Note use of <see cref="JSONView.GenerateEmergencyResult"/> in case of an exception occurring.
        /// (In other words this method tries to always return some useful information)
        /// 
        /// There are three levels of packaging HTML information:
        /// <see cref="HTMLView.GenerateResult"/>
        ///   <see cref="HTMLView.GetHTMLStart"/>
        ///   <see cref="Result.ToHTMLDetailed"/>
        ///     <see cref="BaseEntity.ToHTMLDetailed"/> (called from <see cref="Result.ToHTMLDetailed"/>)
        ///     <see cref="Result.ToHTMLDetailed"/> (actual result, inserts itself at "!--DELIMITER--" left by <see cref="BaseEntity.ToHTMLDetailed"/>). 
        ///     <see cref="BaseEntity.ToHTMLDetailed"/> (called from <see cref="Result.ToHTMLDetailed"/>)
        ///   <see cref="HTMLView.GetHTMLEnd"/>
        /// 
        /// </summary>
        /// <returns></returns>
        public override object GenerateResult() {
            try {
                var html = new StringBuilder();
                html.Append(GetHTMLStart());
                if (Request.Result == null) {
                    html.Append("<p>ERROR: No result-object available, very unexpected</p>");
                } else {
                    html.Append(Request.Result.ToHTMLDetailed(Request));
                }
                html.Append(GetHTMLEnd());

                /// TODO: Add support for headers and location (see both <see cref="HTMLView.GenerateResult"/> and <see cref="CSVView.GenerateResult"/>)
                string location = null;
                Dictionary<string, string> headers = null;
                if (!string.IsNullOrEmpty(location)) {
                    if (headers == null) headers = new Dictionary<string, string>();
                    headers.AddValue("Location", location, () => "You may not combine parameter " + nameof(location) + " together with key 'Location' in " + nameof(headers));
                }
                var retval = new System.Net.Http.HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK, Content = new System.Net.Http.StringContent(html.ToString(), Encoding.UTF8, "text/html") };
                if (headers != null) headers.ForEach(e => retval.Headers.Add(e.Key, e.Value));
                return retval;
            } catch (Exception ex) {
                Util.LogException(ex);
                return JSONView.GenerateEmergencyResult(ResultCode.exception_error, "An exception of type " + ex.GetType() + " occurred in " + GetType() + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + ". See logfile on server for details"); // Details: " + Util.GetExeptionDetails(ex)); // Careful, do not give out details now
            }
        }

        /// <summary>
        /// TODO: Make this static. 
        /// TODO: Make configurable through <see cref="Util.Configuration"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual string GetHTMLStart() =>
            "<html><head>\r\n" +
            "<title>AgoRapide HTML " +
            (Request.CurrentUser == null ? "" : "(" + Request.CurrentUser.IdFriendly + ")") +
            "</title>\r\n" +
            "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + Util.Configuration.C.RootUrl + Util.Configuration.C.CSSRelativePath + "\">\r\n" +
            string.Join("", Util.Configuration.C.ScriptRelativePaths.Select(s => "<script src=\"" + Util.Configuration.C.RootUrl + s + "\"></script>\r\n")) +
                "</head>\r\n" +
            "<body>\r\n" +
            "<div style=\"display:none\" id=\"hiddenDiv\"></div>" + // Dummy element used for JQuery html / text conversion

            // TODO: Make this static. 
            "<h1><a href=\"" + Util.Configuration.C.RootUrl + "\">AgoRapide</a></h1>\r\n" +

            // TODO: Make this static. 
            // TODO: Find better name for #generalQueryResult
            @"<script>
                // Adds BR-tags to text after conversion to  HTML
                function convertToHTML(text) {
                   return $('#hiddenDiv').text(text).html().replace(/(?:\r\n|\r|\n)/g, '<br/>')
                }

                function errorReporter(text) {
                    // TODO: Create nice element in HTML page for reporting errors.                    
                    $('#generalQueryResult').html(convertToHTML(text)); 
                } 

                com.AgoRapide.AgoRapide.setErrorReporter(errorReporter);

                function generalQuery() {
                    com.AgoRapide.AgoRapide.generalQuery({
                        generalQueryId: $('#generalQueryId').val(),
                        statusHtml: function(html) {
                            $('#generalQueryResult').html(html);
                        }
                    });
                } 
            </script>" +

            // TODO: Make this static. 
            "<script>\r\n" + /// TODO: Consider making any <see cref="APIMethod"/> create Javascript such as this automatically...
            "function " + CoreAPIMethod.UpdateProperty + "(keyHTML, entityType, entityId, keyDB) {\r\n" +
            "   var inputId = '#input_' + keyHTML;\r\n" +
            "   var errorId = '#error_' + keyHTML;\r\n" +
            "   $(errorId).html('');\r\n" +
            "\r\n" +
            "   $('#generalQueryResult').html('Saving...');\r\n" + // TODO: Find better name for #generalQueryResult
            "   $(inputId).css({\"background - color\":\"lightgray\"});\r\n" + // TODO: Add some CSS-class here, like "saveInProgress" or similar
            "   com.AgoRapide.AgoRapide.call({\r\n" + /// TODO: Consider making any <see cref="APIMethod"/> create Javascript such as this automatically...
            "     log: true,\r\n" +
            "     url: entityType + '/" + CoreAPIMethod.UpdateProperty + "',\r\n" +
            "     type: '" + HTTPMethod.POST + "',\r\n" +
            "     data: '" + CoreP.QueryId + "=' + entityId + '&" + CoreP.Key + "=' + encodeURIComponent(keyDB) + '&" + CoreP.Value + "=' + encodeURIComponent($(inputId).val()),\r\n" +
            "     success: function saveSuccess(data) {\r\n" +
            "        $('#generalQueryResult').html('');\r\n" +
            "        $(inputId).css({\"background-color\":\"lightgreen\"});\r\n" +
            "     },\r\n" + // TODO: Add some CSS-class here, like "saveSuccess" or similar
            "     error: function saveError(text) {\r\n" + // Note use of com.AgoRapide.AgoRapide.setErrorReporter for general communication of the same error information
            "        $(errorId).html(convertToHTML(text));\r\n" +
            "        $(inputId).css({\"background-color\":\"red\"});\r\n" +
            "     }\r\n" + // TODO: Add some CSS-class here, like "saveError" or similar
            "   });\r\n" +
            "}\r\n" +
            "</script>\r\n" +

            "<input name=\"generalQueryId\" id=\"generalQueryId\" placeholder=\"<Search>\" onkeyup=" +
                "\"try { generalQuery(); } catch (err) { com.AgoRapide.AgoRapide.log(err); } return false;\"" +
            "/>&nbsp;" +
            "<label name=\"generalQueryResult\" id=\"generalQueryResult\"></label>&nbsp;&nbsp;" + // TODO: Find better name for #generalQueryResult

            // TODO: Make ALL ABOVE static

            (Request.CurrentUser == null ? "" :
                /// TODO: Create better HTML-layout. Move to upper right corner for instance
                "<p>" + Request.API.CreateAPILink(Request.CurrentUser
                    //CoreAPIMethod.EntityIndex,
                    //nameof(Request.CurrentUser) + ": " + Request.CurrentUser.IdFriendly,
                    //Request.CurrentUser.GetType(),
                    //new QueryIdInteger(Request.CurrentUser.Id)
                    ) +
                "</p>"
            ) +
            (Request.CurrentUser == null || Request.CurrentUser.RepresentedByEntity == null ? "" :
                /// Logout is equivalent to <see cref="PropertyOperation.SetInvalid"/> for <see cref="CoreP.EntityToRepresent"/>
                /// TODO: Consider creating a <see cref="CoreAPIMethod"/>.Logout in which to hide this code
                /// TODO: Create better HTML-layout. Move to upper right corner for instance
                "<p>" + Request.API.CreateAPILink(
                    CoreAPIMethod.PropertyOperation,
                    "End representation as " + Request.CurrentUser.IdFriendly,
                    typeof(Property),
                    new QueryIdInteger( /// TryGetValue because if we just did <see cref="PropertyOperation.SetInvalid"/> then <see cref="CoreP.EntityToRepresent"/> no longer exists for CurrentUser.
                        (Request.CurrentUser.RepresentedByEntity.Properties.TryGetValue(CoreP.EntityToRepresent, out var p) ? p.Id : 0)),
                    PropertyOperation.SetInvalid
                    ) +
                "</p>"
            ) +
            (Request.CurrentUser == null || Request.CurrentUser.RepresentedByEntity != null ? "" : // Create logout link
                "<p>" + Request.API.CreateAPILink(    /// TODO: Consider creating <see cref="CoreAPIMethod"/>.Logout in which to hide this code                                                                               
                    CoreAPIMethod.UpdateProperty,     /// TODO: Create better HTML-layout. Move to upper right corner for instance
                    "Logout as " + Request.CurrentUser.IdFriendly,
                    Request.CurrentUser.GetType(),
                    new QueryIdInteger(Request.CurrentUser.Id),
                    CoreP.RejectCredentialsNextTime.A(),
                    true
                ) + "</p>"
            ) +
            "<p>" +
                Request.API.CreateAPILink(
                    CoreAPIMethod.Context, CoreAPIMethod.Context.ToString(), (Type)null
                ) +
                (Request.CurrentUser == null ? "" : // Suggest Context for all entities
                    string.Join("", Context.GetAllRelatedTypes(Request.CurrentUser.PV(CoreP.Context.A(), new List<Context>())).Select(t =>
                       "&nbsp;" +
                       Request.API.CreateAPILink(
                       CoreAPIMethod.EntityIndex,
                       t.ToStringVeryShort(), /// TODO: Consider using <see cref="Extensions.ToStringDB"/> instead
                        t,
                       new QueryIdContext()
                   )))
                ) +
            "</p>"
            ;

        /// <summary>
        /// TODO: Make configurable through <see cref="Util.Configuration"/>
        /// </summary>
        /// <returns></returns>
        public virtual string GetHTMLEnd() =>
            Request.Method.MA.CoreMethod == CoreAPIMethod.RootIndex ? "" : ( /// <see cref="CoreAPIMethod.RootIndex"/> is only presented as <see cref="ResponseFormat.HTML"/>
            "<br>\r\n" +
            "<p><a href=\"" + Request.JSONUrl + "\">" + ResponseFormat.JSON + " format for this request</a></p>\r\n<p>" +
            "<p><a href=\"" + Request.CSVUrl + "\">" + ResponseFormat.CSV + " format for this request</a></p>\r\n<p>" +
            (Request.CurrentUser != null ? "" : NotLoggedInExplanation) +
            "</body>\r\n" +
            "</html>\r\n");

        private static string _notLoggedInExplanation;
        private static string NotLoggedInExplanation => _notLoggedInExplanation ?? (_notLoggedInExplanation =
            Documentator.ReplaceKeys((
                "You are not logged in. " +
                "Access is limited to methods with -" + nameof(CoreP.AccessLevelUse) + "- = -" + nameof(AccessLevel.Anonymous) + "-.").HTMLEncode()));
    }
}