using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapide.API {

    [Class(Description = "Generates -" + nameof(ResponseFormat.CSV) + "--view of results.")]
    class CSVView : BaseView {
        public CSVView(Request request) : base(request) { }

        /// <summary>
        /// Note use of <see cref="JSONView.GenerateEmergencyResult"/> in case of an exception occurring.
        /// (In other words this method tries to always return some useful information)
        /// 
        /// There are three levels of packaging CSV information.
        /// <see cref="CSVView.GenerateResult"/>
        ///   <see cref="CSVView.GetCSVStart"/>
        ///   <see cref="Result.ToCSVDetailed"/>
        ///     <see cref="BaseEntity.ToCSVDetailed"/> (called from <see cref="Result.ToCSVDetailed"/>)
        ///     <see cref="Result.ToCSVDetailed"/> (actual result, inserts itself at "!--DELIMITER--" left by <see cref="BaseEntity.ToCSVDetailed"/>). 
        ///     <see cref="BaseEntity.ToCSVDetailed"/> (called from <see cref="Result.ToCSVDetailed"/>)
        ///   <see cref="CSVView.GetCSVEnd"/>
        /// </summary>
        /// <returns></returns>
        public override object GenerateResult() {
            try {
                var csv = new StringBuilder();
                csv.Append(GetCSVStart());
                if (Request.Result == null) {
                    csv.Append("<p>ERROR: No result-object available, very unexpected</p>");
                } else {
                    csv.Append(Request.Result.ToCSVDetailed(Request));
                }
                csv.Append(GetCSVEnd());

                /// TODO: Add support for headers and location (see both <see cref="HTMLView.GenerateResult"/> and <see cref="CSVView.GenerateResult"/>)
                string location = null;
                Dictionary<string, string> headers = null;
                if (!string.IsNullOrEmpty(location)) {
                    if (headers == null) headers = new Dictionary<string, string>();
                    headers.AddValue("Location", location, () => "You may not combine parameter " + nameof(location) + " together with key 'Location' in " + nameof(headers));
                }

                var retval = new System.Net.Http.HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK, Content = new System.Net.Http.StringContent(csv.ToString(), Encoding.UTF8, "text/csv") };
                retval.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") {
                    FileName = Request.Method.MA.Id.IdFriendly + ".CSV" }; // TOOD: Improve on filename, make more specific
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
        public virtual string GetCSVStart() =>
            "AgoRapide CSV\r\n" +
            Util.Configuration.C.RootUrl + "\r\n" +
            (Request.CurrentUser == null ? "" : (nameof(Request.CurrentUser) + Request.CSVFieldSeparator + Request.CurrentUser.IdFriendly + Request.CSVFieldSeparator + Request.API.CreateAPIUrl(Request.CurrentUser) + "\r\n")) +
            ((Request.CurrentUser == null || Request.CurrentUser.RepresentedByEntity == null) ? "" : (nameof(BaseEntity.RepresentedByEntity) + Request.CSVFieldSeparator + Request.CurrentUser.RepresentedByEntity.IdFriendly + Request.CSVFieldSeparator + Request.API.CreateAPIUrl(Request.CurrentUser.RepresentedByEntity) + "\r\n"));

        /// <summary>
        /// TODO: Make configurable through <see cref="Util.Configuration"/>
        /// </summary>
        /// <returns></returns>
        public virtual string GetCSVEnd() =>
            ResponseFormat.JSON + " format for this request" + Request.CSVFieldSeparator + Request.JSONUrl + "\r\n" +
            ResponseFormat.HTML + " format for this request" + Request.CSVFieldSeparator + Request.HTMLUrl + "\r\n" +
            "Generated " + DateTime.Now.ToString(DateTimeFormat.DateHourMin);
    }
}