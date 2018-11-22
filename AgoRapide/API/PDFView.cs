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
        /// There are three levels of packaging PDF information (or actually TEX, since that is the intermediate format used)
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
                var TEX = new StringBuilder();
                TEX.Append(GetPDFStart());
                if (Request.Result == null) {
                    TEX.Append("<p>ERROR: No result-object available, very unexpected</p>");
                } else {
                    TEX.Append(Request.Result.ToPDFDetailed(Request));
                }
                TEX.Append(GetPDFEnd());

                // Save file
                var filename = System.IO.Path.GetTempFileName();
                System.IO.File.WriteAllText(filename + ".tex", TEX.ToString(), Encoding.UTF8);

                // Inspiration from https://stackoverflow.com/questions/5367557/how-to-parse-command-line-output-from-c/5367686#5367686
                var cmdStartInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = @"C:\Windows\System32\cmd.exe",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var cmdProcess = new System.Diagnostics.Process() {
                    StartInfo = cmdStartInfo,
                };

                var dataReceived = new StringBuilder();
                void cmd_DataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e) {
                    dataReceived.Append(e.Data + "\r\n");
                }

                var errorReceived = new StringBuilder();
                void cmd_Error(object sender, System.Diagnostics.DataReceivedEventArgs e) {
                    errorReceived.Append(e.Data + "\r\n"); // Careful, we are probably on "another" thread and throwing an Exception now at once will bring down the whole application. Therefore, just store the information for the time being.
                }

                cmdProcess.ErrorDataReceived += cmd_Error;
                cmdProcess.OutputDataReceived += cmd_DataReceived;
                cmdProcess.EnableRaisingEvents = true;
                cmdProcess.Start();

                cmdProcess.BeginOutputReadLine();
                cmdProcess.BeginErrorReadLine();

                cmdProcess.StandardInput.WriteLine(@"cd " + System.IO.Path.GetDirectoryName(filename));
                cmdProcess.StandardInput.WriteLine(
                    "\"" + @"C:\Users\Bjorn\AppData\Local\Programs\MiKTeX 2.9\miktex\bin\x64\pdflatex" + "\" " +
                    System.IO.Path.GetFileName(filename) + ".tex");

                Exception getException(string message) {
                    return new PDFCompilationException(
                        message + "\r\n-------------------------\r\n\r\n" +
                        "Filename: " + filename + "\r\n" +
                        "ErrorReceived: " + errorReceived + "\r\n\r\n" +
                        "DataReceived: " + dataReceived);
                }
                void bestEfforFailsafeTerminator() { // Try to ensure that process will terminate, ignore any exceptions
                    try {
                        cmdProcess.StandardInput.WriteLine((char)26); // Terminate pdflatex (if still running)
                        cmdProcess.StandardInput.WriteLine("exit"); // Terminate System32\cmd.exe
                    } catch (Exception) {
                        // Ignore exception
                    }
                }
                var i = 0;
                while (true) {
                    var data = dataReceived.ToString();
                    if (data.Contains("Output written on") && data.Contains("Transcript written on")) {
                        bestEfforFailsafeTerminator();
                        cmdProcess.StandardInput.WriteLine("exit");
                        break;
                    }

                    if (errorReceived.Length > 0) {
                        System.Threading.Thread.Sleep(500); // Ensure complete error is "read"
                        bestEfforFailsafeTerminator();
                        throw getException("Error received");
                    }

                    if (
                        data.Contains(" ==> Fatal error occurred") || 
                        data.Contains("Type  H <return>  for immediate help.") ||
                        data.Contains("! Undefined control sequence")
                        ) {
                        System.Threading.Thread.Sleep(500); // Ensure complete data is "read"
                        bestEfforFailsafeTerminator();
                        throw getException("Error message received from pdflatex");
                    }

                    System.Threading.Thread.Sleep(500);
                    if ((i++) > 20) { // Timeout after 10 sec.
                        bestEfforFailsafeTerminator();
                        throw getException("Compilation timeout");
                    }
                }
                i = 0;
                while (true) {
                    if (cmdProcess.HasExited) break;
                    System.Threading.Thread.Sleep(500);
                    if ((i++) > 2) { // Timeout
                        bestEfforFailsafeTerminator();
                        throw getException(@"Timeout waiting for System32\cmd.exe to exit");
                    }
                }
                cmdProcess.Close();

                if (!System.IO.File.Exists(filename + ".pdf")) {
                    throw getException("PDF not found (" + filename + ".pdf" + ")");
                }

                /// TODO: Add support for headers and location (see both <see cref="HTMLView.GenerateResult"/> and <see cref="PDFView.GenerateResult"/>)
                string location = null;
                Dictionary<string, string> headers = null;
                if (!string.IsNullOrEmpty(location)) {
                    if (headers == null) headers = new Dictionary<string, string>();
                    headers.AddValue("Location", location, () => "You may not combine parameter " + nameof(location) + " together with key 'Location' in " + nameof(headers));
                }

                var retval = new System.Net.Http.HttpResponseMessage() {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new System.Net.Http.ByteArrayContent(System.IO.File.ReadAllBytes(filename + ".pdf"))
                };
                new List<string> { filename, filename + ".tex", filename + ".pdf" }.ForEach(f => {
                    try {
                        System.IO.File.Delete(f);
                    } catch (Exception ex) {
                        getException(
                            "Exception " + ex.GetType() + "\r\n" +
                            "with message " + ex.Message +"\r\n" +
                            "occurred when attempting to delete file " + f);
                    }
                });

                retval.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") {
                    FileName = Request.Method.MA.Id.IdFriendly + ".pdf"
                }; // TOOD: Improve on filename, make more specific
                if (headers != null) headers.ForEach(e => retval.Headers.Add(e.Key, e.Value));
                return retval;
            } catch (Exception ex) {
                Util.LogException(ex);
                return JSONView.GenerateEmergencyResult(ResultCode.exception_error, "An exception of type " + ex.GetType() + " occurred in " + GetType() + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + ". See logfile on server for details"); // Details: " + Util.GetExeptionDetails(ex)); // Careful, do not give out details now
            }
        }

        public class PDFCompilationException : ApplicationException {
            public PDFCompilationException(string message) : base(message) { }
            public PDFCompilationException(string message, Exception inner) : base(message, inner) { }
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
            \usepackage{graphicx}
            \usepackage{hyperref}
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
            Util.Configuration.C.RootUrl + "\r\n\r\n" +
            (Request.CurrentUser == null ? "" : (nameof(Request.CurrentUser) + Request.PDFFieldSeparator + Request.CurrentUser.IdFriendly + Request.PDFFieldSeparator + Request.API.CreateAPIUrl(Request.CurrentUser) + "\r\n\r\n")) +
            ((Request.CurrentUser == null || Request.CurrentUser.RepresentedByEntity == null) ? "" : (nameof(BaseEntity.RepresentedByEntity) + Request.PDFFieldSeparator + Request.CurrentUser.RepresentedByEntity.IdFriendly + Request.PDFFieldSeparator + Request.API.CreateAPIUrl(Request.CurrentUser.RepresentedByEntity) + "\r\n\r\n")) +
            "URL" + Request.PDFFieldSeparator + Request.URL + "\r\n\r\n" +
            "Generated " + DateTime.Now.ToString(DateTimeFormat.DateHourMin) +
            @"\pagebreak";

        /// <summary>
        /// TODO: Make configurable through <see cref="Util.Configuration"/>
        /// </summary>
        /// <returns></returns>
        public virtual string GetPDFEnd() =>
            @"\pagebreak" + "\r\n" + // This information may have limited value.
            ResponseFormat.JSON + "-format for this request" + Request.PDFFieldSeparator + Request.JSONUrl + "\r\n\r\n" +
            ResponseFormat.HTML + "-format for this request" + Request.PDFFieldSeparator + Request.HTMLUrl + "\r\n\r\n" +
            ResponseFormat.PDF + "-format for this request" + Request.PDFFieldSeparator + Request.CSVUrl + "\r\n\r\n" +
            // "Generated " + DateTime.Now.ToString(DateTimeFormat.DateHourMin) + // Moved to GetPDFStart instead
            @"
            \end{flushleft}
            \end{document}
            ";
    }
}