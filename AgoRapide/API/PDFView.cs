using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapide.API {

    [Class(Description = "Generates -" + nameof(ResponseFormat.PDF) + " of results.")]
    class PDFView : BaseView {
        public PDFView(Request request) : base(request) { }

        /// <summary>
        /// Note use of <see cref="JSONView.GenerateEmergencyResult"/> in case of an exception occurring.
        /// (In other words this method tries to always return some useful information)
        /// 
        /// There are three levels of packaging PDF information.
        /// <see cref="PDFView.GenerateResult"/>
        ///   <see cref="PDFView.GetPDFStart"/>
        ///   <see cref="Result.ToPDFDetailed"/>
        ///     <see cref="BaseEntity.ToPDFDetailed"/> (called from <see cref="Result.ToPDFDetailed"/>)
        ///     <see cref="Result.ToPDFDetailed"/> (actual result, inserts itself at "!--DELIMITER--" left by <see cref="BaseEntity.ToPDFDetailed"/>). 
        ///     <see cref="BaseEntity.ToPDFDetailed"/> (called from <see cref="Result.ToPDFDetailed"/>)
        ///   <see cref="PDFView.GetPDFEnd"/>
        /// </summary>
        /// <returns></returns>
        public override object GenerateResult() {
            try {
                var PDF = new StringBuilder();
                PDF.Append(GetPDFStart());
                if (Request.Result == null) {
                    PDF.Append("<p>ERROR: No result-object available, very unexpected</p>");
                } else {
                    PDF.Append(Request.Result.ToPDFDetailed(Request));
                }
                PDF.Append(GetPDFEnd());

                /// TODO: Add support for headers and location (see both <see cref="HTMLView.GenerateResult"/> and <see cref="PDFView.GenerateResult"/>)
                string location = null;
                Dictionary<string, string> headers = null;
                if (!string.IsNullOrEmpty(location)) {
                    if (headers == null) headers = new Dictionary<string, string>();
                    headers.AddValue("Location", location, () => "You may not combine parameter " + nameof(location) + " together with key 'Location' in " + nameof(headers));
                }

                var retval = new System.Net.Http.HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK, Content = new System.Net.Http.StringContent(PDF.ToString(), Encoding.UTF8, "text/PDF") };
                retval.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") {
                    FileName = Request.Method.MA.Id.IdFriendly + ".PDF"
                }; // TOOD: Improve on filename, make more specific
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
        public virtual string GetPDFStart() =>
            @"
            \documentclass[a4paper,12pt]{article}
            \usepackage[utf8]{inputenc}
            \usepackage{amsmath}
            \usepackage{graphicx} 
            \setlength{\parindent}{0.0in}
            \setlength{\parskip}{0.25in}
            \setlength{\topmargin}{-0.6in}
            \setlength{\oddsidemargin}{-0.3in}
            \setlength{\evensidemargin}{-0.3in}
            \setlength{\textheight}{260mm}
            \setlength{\textwidth}{180mm}
            \begin{document}
            \begin{flushleft}
            " +
            "\\section*{AgoRapide PDF}\r\n" +
            Util.Configuration.C.RootUrl + "\r\n" +
            (Request.CurrentUser == null ? "" : (nameof(Request.CurrentUser) + Request.PDFFieldSeparator + Request.CurrentUser.IdFriendly + Request.PDFFieldSeparator + Request.API.CreateAPIUrl(Request.CurrentUser) + "\r\n")) +
            ((Request.CurrentUser == null || Request.CurrentUser.RepresentedByEntity == null) ? "" : (nameof(BaseEntity.RepresentedByEntity) + Request.PDFFieldSeparator + Request.CurrentUser.RepresentedByEntity.IdFriendly + Request.PDFFieldSeparator + Request.API.CreateAPIUrl(Request.CurrentUser.RepresentedByEntity) + "\r\n")) +
            "URL" + Request.PDFFieldSeparator + Request.URL + "\r\n";

        /// <summary>
        /// TODO: Make configurable through <see cref="Util.Configuration"/>
        /// </summary>
        /// <returns></returns>
        public virtual string GetPDFEnd() =>
            ResponseFormat.JSON + "-format for this request" + Request.PDFFieldSeparator + Request.JSONUrl + "\r\n" +
            ResponseFormat.HTML + "-format for this request" + Request.PDFFieldSeparator + Request.HTMLUrl + "\r\n" +
            ResponseFormat.PDF + "-format for this request" + Request.PDFFieldSeparator + Request.CSVUrl + "\r\n" +
            "Generated " + DateTime.Now.ToString(DateTimeFormat.DateHourMin) +
            @"
            \end{flushleft}
            \end{document}
            ";
    }
}